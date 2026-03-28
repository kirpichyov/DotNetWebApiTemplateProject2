using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SampleProject.Application.Constants;
using SampleProject.Application.Contracts;
using SampleProject.Application.Mapping;
using SampleProject.Application.Utils;
using SampleProject.Application.Models.Auth;
using SampleProject.Application.Models.Users;
using SampleProject.Application.Security;
using SampleProject.Core.Exceptions;
using SampleProject.Core.Models.Entities;
using SampleProject.Core.Models.Enums;
using SampleProject.Core.Options;
using SampleProject.Core.Utils;
using SampleProject.DataAccess.Connection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace SampleProject.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IValidatorFactory _validatorFactory;
    private readonly IHashingProvider _hashingProvider;
    private readonly DatabaseContext _databaseContext;
    private readonly AuthOptions _authOptions;
    private readonly ISecurityContext _securityContext;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IValidatorFactory validatorFactory,
        IHashingProvider hashingProvider,
        DatabaseContext databaseContext,
        IOptions<AuthOptions> authOptions,
        ISecurityContext securityContext,
        ILogger<AuthService> logger)
    {
        _validatorFactory = validatorFactory;
        _hashingProvider = hashingProvider;
        _databaseContext = databaseContext;
        _securityContext = securityContext;
        _logger = logger;
        _authOptions = authOptions.Value;
    }

    public async Task<CurrentUserDataResponse> SignUp(SignUpRequest request)
    {
        _validatorFactory.ValidateAndThrow(request);

        var usernameExists = await _databaseContext.Users
            .AnyAsync(u => u.Username == request.Username);

        if (usernameExists)
        {
            throw new ValidationFailedException("Username already exists",
                new DetailsBuilder()
                    .Add("username", request.Username)
                    .Build());
        }

        var passwordHash = _hashingProvider.Hash(request.Password);

        var user = User.Create(
            request.Username,
            request.FullName,
            passwordHash,
            Role.User);

        _databaseContext.Users.Add(user);
        await _databaseContext.SaveChangesAsync();

        return user.ToCurrentUserDataResponse();
    }

    public async Task<JwtAuthResponse> SignIn(SignInRequest request)
    {
        _validatorFactory.ValidateAndThrow(request);

        var user = await _databaseContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null)
        {
            throw new ValidationFailedException("Credentials are invalid",
                new DetailsBuilder()
                    .Add("username", request.Username)
                    .Build());
        }

        var hashMatches = _hashingProvider.Verify(request.Password, user.PasswordHash);

        if (!hashMatches)
        {
            throw new ValidationFailedException("Credentials are invalid",
                new DetailsBuilder()
                    .Add("username", request.Username)
                    .Build());
        }

        var accessToken = GenerateAccessToken(user);

        if (request.AuthType is AuthTypeModel.AccessTokenOnly)
        {
            return ToJwtAuthResponse(user, accessToken);
        }

        var (refreshTokenObject, refreshTokenPlain) = GenerateRefreshToken(user, accessToken);

        _databaseContext.RefreshTokens.Add(refreshTokenObject);
        await _databaseContext.SaveChangesAsync();

        return ToJwtAuthResponse(user, accessToken, refreshTokenPlain, refreshTokenObject);
    }

    public async Task DeactivateRefreshToken(ExpireRefreshTokenRequest request)
    {
        _validatorFactory.ValidateAndThrow(request);

        var refreshTokenHash = _hashingProvider.HashSha256(request.RefreshToken);
        var accessTokenHash = _hashingProvider.HashSha256(request.AccessToken);

        var refreshToken = await _databaseContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.RefreshTokenHash == refreshTokenHash &&
                                  rt.AccessTokenHash == accessTokenHash &&
                                  rt.IsActive);

        if (refreshToken is null)
        {
            return;
        }

        if (refreshToken.IsExpired(DateTime.UtcNow))
        {
            return;
        }

        refreshToken.Deactivate(RefreshTokenDeactivationReason.LoggedOut);
        await _databaseContext.SaveChangesAsync();
    }

    public async Task ChangePassword(ChangePasswordRequest request)
    {
        _validatorFactory.ValidateAndThrow(request);

        var userId = _securityContext.GetUserIdOrThrow();

        var user = await _databaseContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new ResourceNotFoundException("User");
        }

        var passwordMatches = _hashingProvider.Verify(request.CurrentPassword, user.PasswordHash);

        if (!passwordMatches)
        {
            throw new ValidationFailedException("Current password does not match");
        }

        var passwordSameAsCurrent = _hashingProvider.Verify(request.NewPassword, user.PasswordHash);

        if (passwordSameAsCurrent)
        {
            throw new ValidationFailedException("New password must be different from the current password");
        }

        var newPasswordHash = _hashingProvider.Hash(request.NewPassword);

        await using var transaction = await _databaseContext.Database.BeginTransactionAsync();

        try
        {
            user.ChangePassword(newPasswordHash);

            if (request.ExpireAllSessions)
            {
                var refreshTokens = await _databaseContext.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.IsActive)
                    .ToArrayAsync();

                foreach (var refreshToken in refreshTokens)
                {
                    refreshToken.Deactivate(RefreshTokenDeactivationReason.PasswordChanged);
                }
            }

            await _databaseContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to change password for user {UserId}", userId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<JwtAuthResponse> RefreshAccessToken(RefreshAccessTokenRequest request)
    {
        _validatorFactory.ValidateAndThrow(request);

        var existingRefreshTokenObject = await ValidateRefreshTokenOrThrow(
            request.AccessToken, request.RefreshToken);

        existingRefreshTokenObject.Deactivate(RefreshTokenDeactivationReason.Refreshed);

        var accessToken = GenerateAccessToken(existingRefreshTokenObject.User);
        var (newRefreshTokenObject, refreshTokenPlain) = GenerateRefreshToken(existingRefreshTokenObject.User, accessToken);

        _databaseContext.RefreshTokens.Add(newRefreshTokenObject);
        await _databaseContext.SaveChangesAsync();

        return ToJwtAuthResponse(existingRefreshTokenObject.User, accessToken, refreshTokenPlain, newRefreshTokenObject);
    }

    public async Task<CurrentUserDataResponse> GetCurrentUserData()
    {
        var userId = _securityContext.GetUserIdOrThrow();

        var user = await _databaseContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new ResourceNotFoundException("User",
                new DetailsBuilder()
                    .Add(DetailsKeys.ResourceId, userId.ToString())
                    .Build());
        }

        return user.ToCurrentUserDataResponse();
    }

    private GeneratedTokenInfo GenerateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrEmpty(_authOptions.Secret))
        {
            throw new InvalidOperationException("JWT secret is not configured");
        }

        var jwtId = Guid.CreateVersion7().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(AuthConstants.UserIdClaim, user.Id.ToString()),
            new(AuthConstants.UsernameClaim, user.Username),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authOptions.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = DateTime.UtcNow.Add(_authOptions.AccessTokenLifetime);

        var token = new JwtSecurityToken(
            issuer: _authOptions.Issuer,
            audience: _authOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new GeneratedTokenInfo(jwtId, tokenString, expiresAtUtc);
    }

    private (RefreshToken Entity, string PlainRefreshToken) GenerateRefreshToken(
        User user,
        GeneratedTokenInfo accessToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(accessToken);

        var refreshTokenPlain = SecretGenerator.GenerateSecret(length: 64);
        var refreshTokenHash = _hashingProvider.HashSha256(refreshTokenPlain);
        var accessTokenHash = _hashingProvider.HashSha256(accessToken.Token);

        var expiresAtUtc = DateTimeOffset.UtcNow.Add(_authOptions.RefreshTokenLifetime);

        var entity = RefreshToken.Create(
            refreshTokenHash,
            accessTokenHash,
            accessToken.Id,
            user.Id,
            expiresAtUtc);

        return (entity, refreshTokenPlain);
    }

    private async Task<RefreshToken> ValidateRefreshTokenOrThrow(string accessToken, string refreshToken)
    {
        var refreshTokenHash = _hashingProvider.HashSha256(refreshToken);
        var accessTokenHash = _hashingProvider.HashSha256(accessToken);

        var existingRefreshTokenEntity = await _databaseContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.RefreshTokenHash == refreshTokenHash &&
                                       rt.AccessTokenHash == accessTokenHash &&
                                       rt.IsActive);

        if (existingRefreshTokenEntity is null)
        {
            throw new ValidationFailedException("Refresh token is invalid or expired");
        }

        if (existingRefreshTokenEntity.IsExpired(DateTime.UtcNow))
        {
            throw new ValidationFailedException("Refresh token is expired");
        }

        if (existingRefreshTokenEntity.User.IsDeleted)
        {
            throw new ValidationFailedException("User associated with the refresh token is deleted");
        }

        return existingRefreshTokenEntity;
    }

    private static JwtAuthResponse ToJwtAuthResponse(User user, GeneratedTokenInfo accessToken)
    {
        return new JwtAuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            AccessToken = new AccessTokenModel
            {
                Token = accessToken.Token,
                ExpiresAtUtc = accessToken.ExpiresAtUtc,
            },
        };
    }

    private static JwtAuthResponse ToJwtAuthResponse(
        User user,
        GeneratedTokenInfo accessToken,
        string refreshTokenPlain,
        RefreshToken refreshTokenEntity)
    {
        return new JwtAuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            AccessToken = new AccessTokenModel
            {
                Token = accessToken.Token,
                ExpiresAtUtc = accessToken.ExpiresAtUtc,
            },
            RefreshToken = new RefreshTokenModel
            {
                Token = refreshTokenPlain,
                ExpiresAtUtc = refreshTokenEntity.ExpiresAtUtc.UtcDateTime,
            },
        };
    }

    private sealed record GeneratedTokenInfo(
        string Id,
        string Token,
        DateTime ExpiresAtUtc);
}

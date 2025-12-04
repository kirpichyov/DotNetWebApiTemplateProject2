using Kirpichyov.FriendlyJwt;
using Kirpichyov.FriendlyJwt.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampleProject.Application.Constants;
using SampleProject.Application.Contracts;
using SampleProject.Application.Mapping;
using SampleProject.Application.Models.Auth;
using SampleProject.Application.Models.Users;
using SampleProject.Application.Security;
using SampleProject.Application.Utils;
using SampleProject.Core.Exceptions;
using SampleProject.Core.Models.Entities;
using SampleProject.Core.Models.Enums;
using SampleProject.Core.Options;
using SampleProject.Core.Utils;
using SampleProject.DataAccess.Connection;

namespace SampleProject.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IValidatorFactory _validatorFactory;
    private readonly IHashingProvider _hashingProvider;
    private readonly DatabaseContext _databaseContext;
    private readonly AuthOptions _authOptions;
    private readonly ISecurityContext _securityContext;
    private readonly IJwtTokenVerifier _jwtTokenVerifier;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IValidatorFactory validatorFactory,
        IHashingProvider hashingProvider,
        DatabaseContext databaseContext,
        IOptions<AuthOptions> authOptions,
        ISecurityContext securityContext,
        IJwtTokenVerifier jwtTokenVerifier,
        ILogger<AuthService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _validatorFactory = validatorFactory;
        _hashingProvider = hashingProvider;
        _databaseContext = databaseContext;
        _securityContext = securityContext;
        _jwtTokenVerifier = jwtTokenVerifier;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
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

        var jwtObject = GenerateAccessToken(user);

        if (request.AuthType is AuthTypeModel.AccessTokenOnly)
        {
            return MapToJwtAuthResponse(user, jwtObject);
        }
        
        var (refreshTokenObject, refreshToken) = GenerateRefreshToken(user, jwtObject.TokenId);

        if (request.AuthType is AuthTypeModel.HttpOnlyCookie)
        {
            _httpContextAccessor.HttpContext!.Response.Cookies.Delete("accessToken");
            _httpContextAccessor.HttpContext!.Response.Cookies.Delete("refreshToken");
            _httpContextAccessor.HttpContext!.Response.Cookies.Delete("userId");
            
            _httpContextAccessor.HttpContext!.Response.Cookies.Append(
                "accessToken",
                jwtObject.Token,
                GetCookieOptions(jwtObject.ExpiresAtUtc));
            
            _httpContextAccessor.HttpContext.Response.Cookies.Append(
                "refreshToken",
                refreshToken,
                GetCookieOptions(refreshTokenObject.ExpiresAtUtc));
            
            _httpContextAccessor.HttpContext.Response.Cookies.Append(
                "userId",
                user.Id.ToString(),
                GetCookieOptions(refreshTokenObject.ExpiresAtUtc, httpOnly: false));
        }
        
        _databaseContext.RefreshTokens.Add(refreshTokenObject);
        await _databaseContext.SaveChangesAsync();
        
        return MapToJwtAuthResponse(user, jwtObject, refreshTokenObject, refreshToken);
    }

    public async Task DeactivateRefreshToken(ExpireRefreshTokenRequest request)
    {
        _validatorFactory.ValidateAndThrow(request);
        
        var verificationResult = _jwtTokenVerifier.Verify(request.AccessToken);
        
        if (!verificationResult.IsValid)
        {
            throw new ValidationFailedException("Access token is invalid");
        }
        
        var userId = verificationResult.UserId;
        
        if (!Guid.TryParse(userId, out var userGuid))
        {
            throw new ValidationFailedException("Access token is invalid");
        }
        
        var refreshTokenHash = _hashingProvider.HashSha256(request.RefreshToken);
        
        var refreshToken = _databaseContext.RefreshTokens
            .FirstOrDefault(rt => rt.RefreshTokenHash == refreshTokenHash &&
                                  rt.UserId == userGuid &&
                                  rt.JwtId == verificationResult.TokenId &&
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
    
    public async Task DeactivateCookieRefreshToken()
    {
        var cookieRefreshToken = _httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(cookieRefreshToken))
        {
            _logger.LogWarning("Refresh token cookie is missing, cannot deactivate refresh token");
            return;
        }
        
        var refreshTokenHash = _hashingProvider.HashSha256(cookieRefreshToken);
        
        var refreshToken = _databaseContext.RefreshTokens
            .FirstOrDefault(rt => rt.RefreshTokenHash == refreshTokenHash &&
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
        
        _httpContextAccessor.HttpContext.Response.Cookies.Delete("accessToken");
        _httpContextAccessor.HttpContext.Response.Cookies.Delete("refreshToken");
        _httpContextAccessor.HttpContext.Response.Cookies.Delete("userId");
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
        
        var jwtObject = GenerateAccessToken(existingRefreshTokenObject.User);
        var (newRefreshTokenObject, refreshToken) = GenerateRefreshToken(existingRefreshTokenObject.User, jwtObject.TokenId);
        
        _databaseContext.RefreshTokens.Add(newRefreshTokenObject);
        await _databaseContext.SaveChangesAsync();
        
        return MapToJwtAuthResponse(existingRefreshTokenObject.User, jwtObject, newRefreshTokenObject, refreshToken);
    }
    
    public async Task<JwtAuthResponse> RefreshCookieAccessToken()
    {
        var refreshToken = _httpContextAccessor.HttpContext!.Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new ValidationFailedException("Refresh token cookie is missing");
        }
        
        var existingRefreshTokenObject = await ValidateRefreshTokenOrThrow(refreshToken);
        
        existingRefreshTokenObject.Deactivate(RefreshTokenDeactivationReason.Refreshed);
        
        var jwtObject = GenerateAccessToken(existingRefreshTokenObject.User);
        var (newRefreshTokenObject, newRefreshToken) = GenerateRefreshToken(existingRefreshTokenObject.User, jwtObject.TokenId);
        
        _databaseContext.RefreshTokens.Add(newRefreshTokenObject);
        await _databaseContext.SaveChangesAsync();
        
        _httpContextAccessor.HttpContext.Response.Cookies.Append(
            "accessToken",
            jwtObject.Token,
            GetCookieOptions(jwtObject.ExpiresAtUtc));
        
        _httpContextAccessor.HttpContext.Response.Cookies.Append(
            "refreshToken",
            newRefreshToken,
            GetCookieOptions(newRefreshTokenObject.ExpiresAtUtc));
        
        _httpContextAccessor.HttpContext.Response.Cookies.Append(
            "userId",
            existingRefreshTokenObject.User.Id.ToString(),
            GetCookieOptions(newRefreshTokenObject.ExpiresAtUtc, httpOnly: false));
        
        return MapToJwtAuthResponse(existingRefreshTokenObject.User, jwtObject, newRefreshTokenObject, newRefreshToken);
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
        
        var jwtObject = new JwtTokenBuilder(_authOptions.AccessTokenLifetime, _authOptions.Secret)
            .WithAudience(_authOptions.Audience)
            .WithIssuer(_authOptions.Issuer)
            .WithUserIdPayloadData(user.Id.ToString())
            .WithUserName(user.Username)
            .Build();
        
        return jwtObject;
    }
    
    private (RefreshToken RefreshTokenObject, string RefreshToken) GenerateRefreshToken(User user, string jwtId)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(jwtId);
        
        var refreshToken = SecretGenerator.GenerateSecret(length: 64);
        var refreshTokenHash = _hashingProvider.HashSha256(refreshToken);
        
        var refreshTokenObject = RefreshToken.Create(
            refreshTokenHash,
            jwtId,
            user.Id,
            DateTimeOffset.UtcNow.Add(_authOptions.RefreshTokenLifetime));
        
        return (refreshTokenObject, refreshToken);
    }

    private async Task<RefreshToken> ValidateRefreshTokenOrThrow(string accessToken, string refreshToken)
    {
        var verificationResult = _jwtTokenVerifier.Verify(accessToken);

        if (!verificationResult.IsValid)
        {
            throw new ValidationFailedException("Access token is invalid");
        }

        var userId = verificationResult.UserId;

        if (!Guid.TryParse(userId, out var userGuid))
        {
            throw new ValidationFailedException("Access token is invalid");
        }

        var refreshTokenHash = _hashingProvider.HashSha256(refreshToken);
        
        var existingRefreshTokenObject = await _databaseContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.RefreshTokenHash == refreshTokenHash &&
                                       rt.UserId == userGuid &&
                                       rt.JwtId == verificationResult.TokenId &&
                                       rt.IsActive);

        if (existingRefreshTokenObject is null)
        {
            throw new ValidationFailedException("Refresh token is invalid or expired",
                new DetailsBuilder()
                    .Add(DetailsKeys.ResourceId, refreshToken)
                    .Build());
        }

        if (existingRefreshTokenObject.IsExpired(DateTime.UtcNow))
        {
            throw new ValidationFailedException("Refresh token is expired",
                new DetailsBuilder()
                    .Add(DetailsKeys.ResourceId, refreshToken)
                    .Build());
        }

        return existingRefreshTokenObject;
    }
    
    private async Task<RefreshToken> ValidateRefreshTokenOrThrow(string refreshToken)
    {
        var refreshTokenHash = _hashingProvider.HashSha256(refreshToken);
        
        var existingRefreshTokenObject = await _databaseContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.RefreshTokenHash == refreshTokenHash &&
                                       rt.IsActive);

        if (existingRefreshTokenObject is null)
        {
            throw new ValidationFailedException("Refresh token is invalid or expired",
                new DetailsBuilder()
                    .Add(DetailsKeys.ResourceId, refreshToken)
                    .Build());
        }

        if (existingRefreshTokenObject.IsExpired(DateTime.UtcNow))
        {
            throw new ValidationFailedException("Refresh token is expired",
                new DetailsBuilder()
                    .Add(DetailsKeys.ResourceId, refreshToken)
                    .Build());
        }

        return existingRefreshTokenObject;
    }

    private static JwtAuthResponse MapToJwtAuthResponse(
        User user,
        GeneratedTokenInfo jwtToken,
        RefreshToken refreshTokenObject,
        string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(jwtToken);
        ArgumentNullException.ThrowIfNull(refreshToken);

        var jwtAuthResponse = MapToJwtAuthResponse(user, jwtToken);

        jwtAuthResponse.RefreshToken = new RefreshTokenModel
        {
            Token = refreshToken,
            ExpiresAtUtc = refreshTokenObject.ExpiresAtUtc.UtcDateTime,
        };
        
        return jwtAuthResponse;
    }
    
    private static JwtAuthResponse MapToJwtAuthResponse(
        User user,
        GeneratedTokenInfo jwtToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(jwtToken);
        
        return new JwtAuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            AccessToken = new AccessTokenModel
            {
                Token = jwtToken.Token,
                ExpiresAtUtc = jwtToken.ExpiresAtUtc,
            }
        };
    }
    
    private static CookieOptions GetCookieOptions(
        DateTimeOffset expiresAtUtc,
        bool httpOnly = true)
    {
        return new CookieOptions
        {
            Expires = expiresAtUtc,
            HttpOnly = httpOnly,
            IsEssential = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
        };
    }
}
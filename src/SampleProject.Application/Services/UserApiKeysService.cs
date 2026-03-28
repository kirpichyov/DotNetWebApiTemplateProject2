using Microsoft.EntityFrameworkCore;
using SampleProject.Application.Constants;
using SampleProject.Application.Contracts;
using SampleProject.Application.Models.ApiKeys;
using SampleProject.Application.Utils;
using SampleProject.Core.Exceptions;
using SampleProject.Core.Models.Entities;
using SampleProject.Core.Utils;
using SampleProject.DataAccess.Connection;

namespace SampleProject.Application.Services;

public sealed class UserApiKeysService : IUserApiKeysService
{
    private readonly DatabaseContext _db;
    private readonly IHashingProvider _hashingProvider;
    private readonly IValidatorFactory _validatorFactory;

    public UserApiKeysService(
        DatabaseContext db,
        IHashingProvider hashingProvider,
        IValidatorFactory validatorFactory)
    {
        _db = db;
        _hashingProvider = hashingProvider;
        _validatorFactory = validatorFactory;
    }

    public async Task<IReadOnlyList<UserApiKeyResponse>> ListAsync(Guid userId)
    {
        var items = await _db.UserApiKeys
            .AsNoTracking()
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToListAsync();

        return items.Select(MapToResponse).ToList();
    }

    public async Task<UserApiKeyResponse> GetByIdAsync(Guid userId, Guid keyId)
    {
        var key = await _db.UserApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId);

        if (key is null)
        {
            throw new ResourceNotFoundException("API key");
        }

        return MapToResponse(key);
    }

    public async Task<CreateUserApiKeyResponse> CreateAsync(Guid userId, CreateUserApiKeyRequest request)
    {
        _validatorFactory.ValidateAndThrow(request);

        var id = Guid.CreateVersion7();
        var secret = SecretGenerator.GenerateSecret(AuthConstants.ApiKey.SecretLength);
        var secretHash = _hashingProvider.Hash(secret);

        var entity = UserApiKey.Create(id, userId, request.Name.Trim(), secretHash);
        _db.UserApiKeys.Add(entity);
        await _db.SaveChangesAsync();

        var fullKey = ApiKeyCredentialParser.FormatFullKey(id, secret);
        return new CreateUserApiKeyResponse
        {
            FullKey = fullKey,
            Key = MapToResponse(entity),
        };
    }

    public async Task<UserApiKeyResponse> UpdateAsync(Guid userId, Guid keyId, UpdateUserApiKeyRequest request)
    {
        _validatorFactory.ValidateAndThrow(request);

        var key = await _db.UserApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId);

        if (key is null)
        {
            throw new ResourceNotFoundException("API key");
        }

        key.Name = request.Name.Trim();
        await _db.SaveChangesAsync();

        return MapToResponse(key);
    }

    public async Task RevokeAsync(Guid userId, Guid keyId)
    {
        var key = await _db.UserApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId);

        if (key is null)
        {
            throw new ResourceNotFoundException("API key");
        }

        if (!key.IsActive)
        {
            return;
        }

        key.Revoke(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync();
    }

    public async Task<CreateUserApiKeyResponse> RotateAsync(Guid userId, Guid keyId)
    {
        var key = await _db.UserApiKeys
            .FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId);

        if (key is null)
        {
            throw new ResourceNotFoundException("API key");
        }

        if (!key.IsActive)
        {
            throw new ValidationFailedException("Cannot rotate a revoked API key");
        }

        var secret = SecretGenerator.GenerateSecret(AuthConstants.ApiKey.SecretLength);
        key.SecretHash = _hashingProvider.Hash(secret);
        key.RevokedAtUtc = null;
        await _db.SaveChangesAsync();

        var fullKey = ApiKeyCredentialParser.FormatFullKey(key.Id, secret);
        return new CreateUserApiKeyResponse
        {
            FullKey = fullKey,
            Key = MapToResponse(key),
        };
    }

    private static UserApiKeyResponse MapToResponse(UserApiKey k)
    {
        return new UserApiKeyResponse
        {
            Id = k.Id,
            Name = k.Name,
            PublicId = ApiKeyCredentialParser.FormatPublicId(k.Id),
            IsActive = k.IsActive,
            CreatedAtUtc = k.CreatedAtUtc,
            RevokedAtUtc = k.RevokedAtUtc,
        };
    }
}

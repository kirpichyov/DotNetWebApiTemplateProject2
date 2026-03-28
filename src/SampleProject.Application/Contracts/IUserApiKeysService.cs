using SampleProject.Application.Models.ApiKeys;

namespace SampleProject.Application.Contracts;

public interface IUserApiKeysService
{
    Task<IReadOnlyList<UserApiKeyResponse>> ListAsync(Guid userId);
    Task<UserApiKeyResponse> GetByIdAsync(Guid userId, Guid keyId);
    Task<CreateUserApiKeyResponse> CreateAsync(Guid userId, CreateUserApiKeyRequest request);
    Task<UserApiKeyResponse> UpdateAsync(Guid userId, Guid keyId, UpdateUserApiKeyRequest request);
    Task RevokeAsync(Guid userId, Guid keyId);
    Task<CreateUserApiKeyResponse> RotateAsync(Guid userId, Guid keyId);
}

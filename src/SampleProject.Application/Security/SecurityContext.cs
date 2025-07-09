using SampleProject.Core.Models.Entities;
using SampleProject.Core.Models.Enums;

namespace SampleProject.Application.Security;

public sealed class SecurityContext : ISecurityContext
{
    public Guid? UserId { get; private set; }
    public Role Role { get; private set; }
    public SecurityContextUserType AuthType { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public bool IsInitialized { get; private set; }
    
    public void Initialize(User user)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Security context is already initialized.");
        }
        
        UserId = user.Id;
        Role = user.Role;
        IsAuthenticated = true;
        IsInitialized = true;
        AuthType = SecurityContextUserType.BearerToken;
    }
    
    public void InitializeAsAnonymous()
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Security context is already initialized.");
        }
        
        Role = Role.None;
        IsAuthenticated = false;
        IsInitialized = true;
        AuthType = SecurityContextUserType.Anonymous;
    }

    public Guid GetUserIdOrThrow()
    {
        if (AuthType is SecurityContextUserType.Unauthorized)
        {
            throw new InvalidOperationException("Security context must be authorized first");
        }
        
        if (UserId is null)
        {
            throw new InvalidOperationException("User ID is not set in the security context");
        }
        
        return UserId.Value;
    }
    
    public bool HasRole(Role role)
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Security context is not initialized");
        }
        
        return Role == role;
    }
}
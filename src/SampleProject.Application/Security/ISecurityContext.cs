using SampleProject.Core.Models.Enums;

namespace SampleProject.Application.Security;

public interface ISecurityContext
{
    Guid? UserId { get; }
    Role Role { get; }
    SecurityContextUserType AuthType { get; }
    bool IsAuthenticated { get; }
    bool IsInitialized { get; }
    
    Guid GetUserIdOrThrow();
    
    bool HasRole(Role role);
}
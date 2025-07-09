using SampleProject.Core.Models.Enums;

namespace SampleProject.Application.Security;

public sealed class AuthorizeRoleAttribute : Attribute
{
    public AuthorizeRoleAttribute(Role role)
    {
        Role = role;
    }

    public Role Role { get; }
}
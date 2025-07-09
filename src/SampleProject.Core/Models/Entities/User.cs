using SampleProject.Core.Models.Enums;

namespace SampleProject.Core.Models.Entities;

public sealed class User : AuditEntity<Guid>
{
    private User()
        : base(Guid.CreateVersion7())
    {
    }
    
    public string Username { get; set; }
    public string FullName { get; set; }
    public string PasswordHash { get; set; }
    public Role Role { get; set; }

    public static User Create(
        string username,
        string fullName,
        string passwordHash,
        Role role)
    {
        var user = new User
        {
            Username = username,
            FullName = fullName,
            PasswordHash = passwordHash,
            Role = role,
        };
        
        return user;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }
}
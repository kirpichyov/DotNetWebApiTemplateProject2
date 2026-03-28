namespace SampleProject.Core.Models.Entities;

public sealed class UserApiKey : AuditEntity<Guid>
{
    public UserApiKey(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string SecretHash { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }

    public User User { get; set; }

    public static UserApiKey Create(Guid id, Guid userId, string name, string secretHash)
    {
        return new UserApiKey(id)
        {
            UserId = userId,
            Name = name,
            SecretHash = secretHash,
            IsActive = true,
        };
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        IsActive = false;
        RevokedAtUtc = revokedAtUtc;
    }
}

using SampleProject.Core.Models.Enums;

namespace SampleProject.Core.Models.Entities;

public sealed class RefreshToken : AuditEntity<Guid>
{
    public RefreshToken()
        : base(Guid.CreateVersion7())
    {
    }
    
    public string RefreshTokenHash { get; set; }
    public string JwtId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public RefreshTokenDeactivationReason DeactivationReason { get; set; }
    
    public User User { get; set; }

    public static RefreshToken Create(
        string refreshTokenHash,
        string jwtId,
        Guid userId,
        DateTimeOffset expiresAtUtc)
    {
        return new RefreshToken
        {
            RefreshTokenHash = refreshTokenHash,
            JwtId = jwtId,
            UserId = userId,
            ExpiresAtUtc = expiresAtUtc,
            IsActive = true,
        };
    }

    public bool IsExpired(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The provided date must be in UTC.", nameof(nowUtc));
        }
        
        return nowUtc >= ExpiresAtUtc.UtcDateTime;
    }

    public void Deactivate(RefreshTokenDeactivationReason deactivationReason)
    {
        IsActive = false;
        DeactivationReason = deactivationReason;
    }
}
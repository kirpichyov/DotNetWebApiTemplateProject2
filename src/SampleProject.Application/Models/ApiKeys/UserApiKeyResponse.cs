namespace SampleProject.Application.Models.ApiKeys;

public sealed class UserApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string PublicId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

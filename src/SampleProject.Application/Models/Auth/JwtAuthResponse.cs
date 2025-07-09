namespace SampleProject.Application.Models.Auth;

public sealed class JwtAuthResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public AccessTokenModel AccessToken { get; set; }
    public RefreshTokenModel RefreshToken { get; set; }
}

public sealed class AccessTokenModel
{
    public string Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class RefreshTokenModel
{
    public string Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
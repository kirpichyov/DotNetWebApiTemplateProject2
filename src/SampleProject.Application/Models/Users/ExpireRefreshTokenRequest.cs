namespace SampleProject.Application.Models.Users;

public sealed class ExpireRefreshTokenRequest
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
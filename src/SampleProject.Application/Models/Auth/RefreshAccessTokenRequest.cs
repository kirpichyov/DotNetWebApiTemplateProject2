namespace SampleProject.Application.Models.Auth;

public sealed class RefreshAccessTokenRequest
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
namespace SampleProject.Application.Models.Auth;

public sealed class SignInRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public AuthTypeModel AuthType { get; set; }
}
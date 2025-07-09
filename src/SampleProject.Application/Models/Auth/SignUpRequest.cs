namespace SampleProject.Application.Models.Auth;

public sealed class SignUpRequest
{
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Password { get; set; }
}
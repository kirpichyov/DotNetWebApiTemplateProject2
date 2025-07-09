namespace SampleProject.Application.Models.Auth;

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public bool ExpireAllSessions { get; set; }
}
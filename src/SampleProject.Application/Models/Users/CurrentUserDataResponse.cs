namespace SampleProject.Application.Models.Users;

public sealed class CurrentUserDataResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public RoleModel Role { get; set; }
}
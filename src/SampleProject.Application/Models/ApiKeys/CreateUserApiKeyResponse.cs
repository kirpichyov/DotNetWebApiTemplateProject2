namespace SampleProject.Application.Models.ApiKeys;

public sealed class CreateUserApiKeyResponse
{
    public string FullKey { get; set; }
    public UserApiKeyResponse Key { get; set; }
}

namespace SampleProject.Core.Contracts;

public interface IAuthContext
{
    bool IsLoggedIn { get; }
    string UserId { get; }
    string Username { get; }
    string[] GetPayloadValues(string key);
    string GetPayloadValueOrDefault(string key);
    (string Key, string Value)[] GetPayloadData();
    Guid GetUserIdAsUuid();
}

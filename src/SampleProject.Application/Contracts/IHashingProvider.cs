namespace SampleProject.Application.Contracts;

public interface IHashingProvider
{
    public string Hash(string value);
    public bool Verify(string value, string hash);
    public string HashSha256(string value);
}
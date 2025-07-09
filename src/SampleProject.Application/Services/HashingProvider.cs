using System.Security.Cryptography;
using System.Text;
using SampleProject.Application.Contracts;

namespace SampleProject.Application.Services;

public sealed class HashingProvider : IHashingProvider
{
    public string Hash(string value)
    {
        return BCrypt.Net.BCrypt.HashPassword(value);
    }

    public bool Verify(string value, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(value, hash);
    }

    public string HashSha256(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
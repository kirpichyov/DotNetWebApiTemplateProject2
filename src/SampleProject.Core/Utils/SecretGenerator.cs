using System.Security.Cryptography;
using System.Text;

namespace SampleProject.Core.Utils;

public static class SecretGenerator
{
    private const string AlphanumericChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "abcdefghijklmnopqrstuvwxyz" +
        "0123456789";

    /// <summary>
    /// Generates a cryptographically-secure random alphanumeric string.
    /// </summary>
    /// <param name="length">Desired length of the output string. Must be non-negative.</param>
    /// <returns>Randomly-generated alphanumeric string.</returns>
    public static string GenerateSecret(int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        }

        var result = new StringBuilder(length);

        for (var i = 0; i < length; i++)
        {
            var idx = RandomNumberGenerator.GetInt32(AlphanumericChars.Length);
            result.Append(AlphanumericChars[idx]);
        }
        
        return result.ToString();
    }
}
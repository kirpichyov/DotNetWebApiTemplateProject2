using System.Globalization;
using SampleProject.Application.Constants;

namespace SampleProject.Application.Utils;

public static class ApiKeyCredentialParser
{
    public static bool TryParse(string credential, out Guid keyId, out string secret)
    {
        keyId = default;
        secret = null;

        if (string.IsNullOrWhiteSpace(credential))
        {
            return false;
        }

        var trimmed = credential.Trim();
        var prefix = AuthConstants.ApiKey.PrefixWithUnderscore;
        if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var dotIdx = trimmed.IndexOf('.');
        if (dotIdx <= 0 || dotIdx >= trimmed.Length - 1)
        {
            return false;
        }

        var left = trimmed[..dotIdx];
        var right = trimmed[(dotIdx + 1)..];

        var idPart = left[prefix.Length..];
        if (idPart.Length != 32)
        {
            return false;
        }

        if (!IsHexString(idPart))
        {
            return false;
        }

        if (!Guid.TryParseExact(idPart, "N", out keyId))
        {
            return false;
        }

        if (right.Length != AuthConstants.ApiKey.SecretLength || !IsAlphanumeric(right))
        {
            return false;
        }

        secret = right;
        return true;
    }

    public static string FormatFullKey(Guid keyId, string secret32)
    {
        return $"{AuthConstants.ApiKey.PrefixWithUnderscore}{keyId:N}.{secret32}";
    }

    public static string FormatPublicId(Guid keyId)
    {
        return $"{AuthConstants.ApiKey.PrefixWithUnderscore}{keyId:N}";
    }

    private static bool IsHexString(string s)
    {
        foreach (var c in s)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAlphanumeric(string s)
    {
        foreach (var c in s)
        {
            if (!char.IsAsciiLetterOrDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}

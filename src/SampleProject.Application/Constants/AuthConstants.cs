namespace SampleProject.Application.Constants;

public static class AuthConstants
{
    public const string UserIdClaim = "userId";
    public const string UsernameClaim = "username";

    public const string BearerOrApiKeyPolicy = "BearerOrApiKey";

    public const string MultiAuthScheme = "MultiAuth";

    public static class ApiKey
    {
        public const string Scheme = "ApiKey";
        public const string Prefix = "apik";
        public const int SecretLength = 32;

        public static string PrefixWithUnderscore => $"{Prefix}_";
    }
}

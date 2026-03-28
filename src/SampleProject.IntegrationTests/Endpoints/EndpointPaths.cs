namespace SampleProject.IntegrationTests.Endpoints;

internal static class EndpointPaths
{
    public const string AuthSignUp = "v1/auth/sign-up";
    public const string AuthSignIn = "v1/auth/sign-in";
    public const string AuthMe = "v1/auth/me";
    public const string AuthRefresh = "v1/auth/refresh-access-token";
    public const string AuthChangePassword = "v1/auth/change-password";
    public const string AuthDeactivateRefresh = "v1/auth/deactivate-refresh-token";

    public const string UserApiKeys = "v1/users/me/api-keys";
    public static string UserApiKeyById(Guid id) => $"v1/users/me/api-keys/{id:D}";
    public static string UserApiKeyRotate(Guid id) => $"v1/users/me/api-keys/{id:D}/rotate";

    public const string ApiKeyDemoMe = "v1/api-key-demo/me";
}

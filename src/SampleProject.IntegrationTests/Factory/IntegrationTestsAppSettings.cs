namespace SampleProject.IntegrationTests.Factory;

public sealed class IntegrationTestsAppSettings
{
    public static readonly KeyValuePair<string, string>[] Settings =
    [
        new("AuthOptions:Issuer", "localhost"),
        new("AuthOptions:Audience", "localhost"),
        new("AuthOptions:Secret", Guid.NewGuid().ToString("D")),
        new("AuthOptions:AccessTokenLifetime", "00:10:00"),
        new("AuthOptions:RefreshTokenLifetime", "24:00:00"),
        new("AuthOptions:AllowedOrigins:0", "http://localhost"),
        new("Logging:Seq:Enabled", "false"),
    ];
}

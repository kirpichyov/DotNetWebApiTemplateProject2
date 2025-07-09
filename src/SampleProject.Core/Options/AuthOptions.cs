namespace SampleProject.Core.Options;

public sealed class AuthOptions
{
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public TimeSpan AccessTokenLifetime { get; init; }
    public TimeSpan RefreshTokenLifetime { get; init; }
    public string Secret { get; init; }
    public string[] AllowedOrigins { get; init; }
}
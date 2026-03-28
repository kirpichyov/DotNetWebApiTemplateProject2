using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SampleProject.Application.Constants;
using SampleProject.Application.Contracts;
using SampleProject.Application.Utils;
using SampleProject.DataAccess.Connection;

namespace SampleProject.Application.Security;

public sealed class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthSchemeOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public ApiKeyAuthHandler(
        IOptionsMonitor<ApiKeyAuthSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceProvider serviceProvider)
        : base(options, logger, encoder)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string apiKey = null;

        var authHeader = Request.Headers.Authorization.ToString();
        var headerParts = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (headerParts.Length == 2 &&
            headerParts[0].Equals(AuthConstants.ApiKey.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            apiKey = headerParts[1].Trim();
        }
        else if (Request.Query.TryGetValue("apiKey", out var queryKey) && !string.IsNullOrEmpty(queryKey))
        {
            apiKey = queryKey.ToString();
        }
        else
        {
            return AuthenticateResult.NoResult();
        }

        if (!ApiKeyCredentialParser.TryParse(apiKey, out var keyId, out var secret))
        {
            return AuthenticateResult.Fail("Invalid API key format.");
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var hashing = scope.ServiceProvider.GetRequiredService<IHashingProvider>();

        var row = await db.UserApiKeys
            .AsNoTracking()
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == keyId && k.IsActive);

        if (row?.User is null)
        {
            return AuthenticateResult.Fail("API key is invalid or revoked.");
        }

        if (!hashing.Verify(secret, row.SecretHash))
        {
            return AuthenticateResult.Fail("API key is invalid.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, row.User.Id.ToString()),
            new(AuthConstants.UserIdClaim, row.User.Id.ToString()),
            new(AuthConstants.UsernameClaim, row.User.Username),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SampleProject.Application.Constants;
using SampleProject.Core.Contracts;

namespace SampleProject.Application.Security;

public sealed class AuthContext : IAuthContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        IsLoggedIn = false;

        if (httpContextAccessor.HttpContext is null)
        {
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (!httpContext.User.Claims.Any())
        {
            return;
        }

        IsLoggedIn = true;
        UserId = GetPayloadValueOrDefault(AuthConstants.UserIdClaim);
        Username = GetPayloadValueOrDefault(AuthConstants.UsernameClaim);
    }

    public bool IsLoggedIn { get; }
    public string UserId { get; }
    public string Username { get; }

    public string[] GetPayloadValues(string key)
    {
        ValidateIfLoggedInAndThrow();

        return (_httpContextAccessor.HttpContext?.User.Claims ?? [])
            .Where(claim => claim.Type == key)
            .Select(claim => claim.Value)
            .ToArray();
    }

    public string GetPayloadValueOrDefault(string key) => RetrieveClaimOrDefault(key)?.Value;

    public (string Key, string Value)[] GetPayloadData()
    {
        ValidateIfLoggedInAndThrow();

        return (_httpContextAccessor.HttpContext?.User.Claims ?? [])
            .Select(claim => (claim.Type, claim.Value))
            .ToArray();
    }

    public Guid GetUserIdAsUuid()
    {
        if (Guid.TryParse(UserId, out var userIdAsGuid))
        {
            return userIdAsGuid;
        }

        throw new FormatException("User id is not in a valid GUID format. Actual value was: " + UserId);
    }

    private Claim RetrieveClaimOrDefault(string key)
    {
        ValidateIfLoggedInAndThrow();

        return (_httpContextAccessor.HttpContext?.User.Claims ?? [])
            .SingleOrDefault(claim => claim.Type == key);
    }

    private void ValidateIfLoggedInAndThrow()
    {
        if (!IsLoggedIn)
        {
            throw new InvalidOperationException("User must be logged in to perform payload reading.");
        }
    }
}

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleProject.Api.Constants;
using SampleProject.Api.Security;
using SampleProject.Application.Constants;
using SampleProject.Application.Contracts;
using SampleProject.Application.Models.Users;
using SampleProject.Application.Security;
using SampleProject.Core.Models.Api;

namespace SampleProject.Api.Endpoints.Internal.v1;

/// <summary>
/// Isolated JWT-free surface to verify <c>Authorization: ApiKey apik_…</c> (or <c>?apiKey=</c>) end-to-end.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("v{version:apiVersion}/api-key-demo")]
[ApiExplorerSettings(GroupName = EndpointConstants.DefaultGroupName)]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = AuthConstants.ApiKey.Scheme)]
[ServiceFilter(typeof(SecurityContextFilter))]
public sealed class ApiKeyDemoController : ControllerBase
{
    private readonly IAuthService _authService;

    public ApiKeyDemoController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMeAsync()
    {
        var user = await _authService.GetCurrentUserData();
        return Ok(user);
    }
}

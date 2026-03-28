using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleProject.Api.Constants;
using SampleProject.Api.Security;
using SampleProject.Application.Constants;
using SampleProject.Application.Contracts;
using SampleProject.Application.Models.ApiKeys;
using SampleProject.Application.Security;
using SampleProject.Core.Models.Api;

namespace SampleProject.Api.Endpoints.Internal.v1;

[ApiController]
[ApiVersion("1")]
[Route("v{version:apiVersion}/users/me/api-keys")]
[ApiExplorerSettings(GroupName = EndpointConstants.DefaultGroupName)]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = AuthConstants.MultiAuthScheme)]
[ServiceFilter(typeof(SecurityContextFilter))]
public sealed class UserApiKeysController : ControllerBase
{
    private readonly IUserApiKeysService _userApiKeysService;
    private readonly ISecurityContext _securityContext;

    public UserApiKeysController(IUserApiKeysService userApiKeysService, ISecurityContext securityContext)
    {
        _userApiKeysService = userApiKeysService;
        _securityContext = securityContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserApiKeyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync()
    {
        var userId = _securityContext.GetUserIdOrThrow();
        var items = await _userApiKeysService.ListAsync(userId);
        return Ok(items);
    }

    [HttpGet("{keyId:guid}")]
    [ProducesResponseType(typeof(UserApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync([FromRoute] Guid keyId)
    {
        var userId = _securityContext.GetUserIdOrThrow();
        var item = await _userApiKeysService.GetByIdAsync(userId, keyId);
        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateUserApiKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateUserApiKeyRequest request)
    {
        var userId = _securityContext.GetUserIdOrThrow();
        var result = await _userApiKeysService.CreateAsync(userId, request);
        return new ObjectResult(result) { StatusCode = StatusCodes.Status201Created };
    }

    [HttpPut("{keyId:guid}")]
    [ProducesResponseType(typeof(UserApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid keyId, [FromBody] UpdateUserApiKeyRequest request)
    {
        var userId = _securityContext.GetUserIdOrThrow();
        var item = await _userApiKeysService.UpdateAsync(userId, keyId, request);
        return Ok(item);
    }

    [HttpDelete("{keyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAsync([FromRoute] Guid keyId)
    {
        var userId = _securityContext.GetUserIdOrThrow();
        await _userApiKeysService.RevokeAsync(userId, keyId);
        return NoContent();
    }

    [HttpPost("{keyId:guid}/rotate")]
    [ProducesResponseType(typeof(CreateUserApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateAsync([FromRoute] Guid keyId)
    {
        var userId = _securityContext.GetUserIdOrThrow();
        var result = await _userApiKeysService.RotateAsync(userId, keyId);
        return Ok(result);
    }
}

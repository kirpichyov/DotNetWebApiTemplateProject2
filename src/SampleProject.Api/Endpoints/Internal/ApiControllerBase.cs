using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleProject.Api.Constants;
using SampleProject.Api.Security;

namespace SampleProject.Api.Endpoints.Internal;

[ApiController]
[Route("v{version:apiVersion}/[controller]")]
[ApiExplorerSettings(GroupName = EndpointConstants.DefaultGroupName)]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(SecurityContextFilter))]
public class ApiControllerBase : ControllerBase
{
    protected ObjectResult Created(object response)
    {
        return new ObjectResult(response) { StatusCode = StatusCodes.Status201Created };
    }
}
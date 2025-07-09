using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SampleProject.Application.Security;

namespace SampleProject.Api.Security;

public sealed class SecurityContextFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var roleAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<AuthorizeRoleAttribute>()
            .FirstOrDefault();

        if (roleAttribute is null)
        {
            return;
        }
        
        var securityContext = context.HttpContext.RequestServices.GetRequiredService<ISecurityContext>();
        if (!securityContext.IsAuthenticated)
        {
            context.Result = new ForbidResult("Bearer");
            return;
        }

        if (!securityContext.HasRole(roleAttribute.Role))
        {
            context.Result = new ForbidResult("Bearer");
            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
using Kirpichyov.FriendlyJwt.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.EntityFrameworkCore;
using SampleProject.Application.Security;
using SampleProject.Core.Models.Enums;
using SampleProject.DataAccess.Connection;
using Serilog.Context;

namespace SampleProject.Api.Security;

public sealed class SecurityContextInitializerMiddleware
{
    private static readonly string[] IgnoredEndpoints =
    [
        "/openapi",
        "/swagger",
        "/health"
    ];
    
    private readonly RequestDelegate _next;

    public SecurityContextInitializerMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        
        if (endpoint == null || endpoint.DisplayName?.StartsWith("405") is true)
        {
            await _next(context);
            return;
        }
        
        var logger = context.RequestServices.GetRequiredService<ILogger<SecurityContextInitializerMiddleware>>();
        var securityContext = context.RequestServices.GetRequiredService<SecurityContext>();
        var jwtTokenReader = context.RequestServices.GetRequiredService<IJwtTokenReader>();
        var databaseContext = context.RequestServices.GetRequiredService<DatabaseContext>();
        
        if (endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() is not null ||
            IgnoredEndpoints.Any(ie => endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()?.Route.StartsWith(ie, StringComparison.OrdinalIgnoreCase) == true))
        {
            securityContext.InitializeAsAnonymous();
            await _next(context);
            return;
        }
        
        if (!jwtTokenReader.IsLoggedIn)
        {
            logger.LogInformation("Request is not authenticated. Skipping security context initialization.");
            
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var userId = Guid.Parse(jwtTokenReader.UserId);
        var user = await databaseContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            logger.LogInformation("User with ID {UserId} not found", userId);
            
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        
        securityContext.Initialize(user);

        var userContextLogData = new UserContextLogData
        {
            UserId = user.Id.ToString(),
            Role = user.Role.ToStringFast()
        };
        
        using var _ = LogContext.PushProperty("UserContext", userContextLogData, destructureObjects: true);
        
        await _next(context);
    }

    private sealed class UserContextLogData
    {
        public string UserId { get; set; }
        public string Role { get; set; }
    }
}
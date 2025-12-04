using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using SampleProject.Api.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SampleProject.Api.Configuration.Swagger;

internal sealed class AuthOperationFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		var attributes = context.MethodInfo.DeclaringType!.GetCustomAttributes(true)
			.Union(context.MethodInfo.GetCustomAttributes(true))
			.ToArray();

		var allowAnonymous = attributes
			.OfType<AllowAnonymousAttribute>()
			.Any();

		var hasAuthorizeAttribute = attributes
			.OfType<AuthorizeAttribute>()
			.Any();

		var apiExplorerAttribute = attributes
			.OfType<ApiExplorerSettingsAttribute>()
			.FirstOrDefault();
		
		if (allowAnonymous || !hasAuthorizeAttribute)
		{
			return;
		}
		
		var id = apiExplorerAttribute?.GroupName switch
		{
			EndpointConstants.DefaultGroupName => "Bearer",
			_ => "Bearer"
		};
		
		var securityRequirement = new OpenApiSecurityRequirement
		{
			{
				new OpenApiSecuritySchemeReference(id, context.Document),
				new List<string>()
			}
		};

		operation.Security = new[] { securityRequirement };

		operation.Responses?.TryAdd(
			((int)HttpStatusCode.Unauthorized).ToString(),
			GetEmptyJsonResponse(nameof(HttpStatusCode.Unauthorized))
		);

		operation.Responses?.TryAdd(
			((int)HttpStatusCode.Forbidden).ToString(),
			GetEmptyJsonResponse(nameof(HttpStatusCode.Forbidden))
		);
	}

	private static OpenApiResponse GetEmptyJsonResponse(string description)
	{
		return new OpenApiResponse
		{
			Content = new Dictionary<string, OpenApiMediaType>
			{
				{
					"application/json",
					new OpenApiMediaType {Schema = new OpenApiSchema {Default = JsonNode.Parse("{}")}}
				}
			},
			Description = description
		};
	}
}

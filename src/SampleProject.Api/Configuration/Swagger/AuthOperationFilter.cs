using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using SampleProject.Api.Constants;
using SampleProject.Application.Constants;
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

		var authorizeAttributes = attributes
			.OfType<AuthorizeAttribute>()
			.ToList();

		if (allowAnonymous || authorizeAttributes.Count == 0)
		{
			return;
		}

		var schemeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var attr in authorizeAttributes)
		{
			if (string.IsNullOrWhiteSpace(attr.AuthenticationSchemes))
			{
				continue;
			}

			foreach (var part in attr.AuthenticationSchemes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
			{
				schemeSet.Add(part);
			}
		}

		List<OpenApiSecurityRequirement> securityRequirements;
		if (schemeSet.Count > 0 &&
		    schemeSet.Contains(AuthConstants.ApiKey.Scheme) &&
		    !schemeSet.Contains(JwtBearerDefaults.AuthenticationScheme))
		{
			securityRequirements =
			[
				new OpenApiSecurityRequirement
				{
					{ new OpenApiSecuritySchemeReference(AuthConstants.ApiKey.Scheme, context.Document), new List<string>() }
				}
			];
		}
		else
		{
			securityRequirements =
			[
				new OpenApiSecurityRequirement
				{
					{ new OpenApiSecuritySchemeReference("Bearer", context.Document), new List<string>() }
				}
			];
		}

		operation.Security = securityRequirements;

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

using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SampleProject.Api.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SampleProject.Api.Configuration.Swagger;

public sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _versionProvider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider versionProvider)
    {
        _versionProvider = versionProvider;
    }

    /// <summary>
    /// Configure each API discovered for Swagger Documentation.
    /// </summary>
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _versionProvider.ApiVersionDescriptions)
        {
            options.SwaggerDoc($"{EndpointConstants.DefaultGroupName}-{description.GroupName}", new OpenApiInfo
            {
                Version = description.ApiVersion.ToString(),
                Title = $"API v{description.ApiVersion}",
            });
        }
        
        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            var groupName = apiDesc.GroupName;
            if (groupName == null) return false;

            // Match the document name with the group name
            return docName == $"{EndpointConstants.DefaultGroupName}-{groupName}";
        });
    }

    /// <summary>
    /// Configure Swagger Options. Inherited from the Interface.
    /// </summary>
    public void Configure(string name, SwaggerGenOptions options)
    {
        Configure(options);
    }

    /// <summary>
    /// Create information about the version of the API.
    /// </summary>
    /// <returns>Information about the API.</returns>
    private OpenApiInfo CreateVersionInfo(ApiVersionDescription desc)
    {
        var info = new OpenApiInfo()
        {
            Title = "Sample Project API",
            Version = desc.ApiVersion.ToString()
        };

        if (desc.IsDeprecated)
        {
            info.Description += " This API version has been deprecated.";
        }

        return info;
    }
}
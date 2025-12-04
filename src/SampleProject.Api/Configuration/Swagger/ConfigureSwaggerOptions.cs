using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
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
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = $"SampleProject API {description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated ? "This API version has been deprecated." : string.Empty
            });
        }

        options.DocInclusionPredicate((docName, apiDesc) => 
            apiDesc.GroupName == null || docName == apiDesc.GroupName);
    }
}
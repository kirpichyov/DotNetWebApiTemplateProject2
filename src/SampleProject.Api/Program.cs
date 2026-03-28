using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SampleProject.Api.Configuration;
using SampleProject.Api.Configuration.Swagger;
using SampleProject.Api.Endpoints.Internal;
using SampleProject.Api.Middleware;
using SampleProject.Api.Security;
using SampleProject.Application;
using SampleProject.Application.Constants;
using SampleProject.Application.Security;
using SampleProject.Core.Options;
using SampleProject.DataAccess;
using SampleProject.Infrastructure.Converters;
using Serilog;
using Serilog.Configuration;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Swashbuckle.AspNetCore.Filters;

const string mainCorsPolicy = "MainPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddTestConfiguration();

builder.Host.UseDefaultServiceProvider((_, options) =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

RegisterOptions(builder.Services, builder.Configuration);
SetupLogging(builder.Services, builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddDataAccessServices(builder.Configuration, builder.Environment);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHttpClient();
AddSecurityServices(builder.Services);

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddEndpointsApiExplorer();

ValidatorOptions.Global.LanguageManager.Enabled = false;
builder.Services.AddValidatorsFromAssemblyContaining<IAssemblyMarker>();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;

    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"),
        new MediaTypeApiVersionReader("x-api-version")
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();

if (!builder.Environment.IsProduction())
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer",
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "Enter JWT token. (Example: 'your_token_here')",
            });

        options.AddSecurityDefinition(AuthConstants.ApiKey.Scheme,
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description =
                    "Use header value: ApiKey apik_<guidN>.<secret32> — or pass the full key in query ?apiKey=",
            });

        options.MapType<DateOnly>(() => new OpenApiSchema()
        {
            Type = JsonSchemaType.String,
            Format = "date",
        });
			
        options.OperationFilter<AuthOperationFilter>();
        options.ExampleFilters();
        
        var xmlFileName = $"{typeof(ApiControllerBase).Assembly.GetName().Name}.xml";
        var xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
        options.IncludeXmlComments(xmlFilePath);
    });
}

builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: mainCorsPolicy,
        policy =>
        {
            var authOptions = builder.Configuration.GetSection(nameof(AuthOptions)).Get<AuthOptions>();
			
            policy.WithOrigins(authOptions.AllowedOrigins);
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            policy.AllowCredentials();
        });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateOnlyJsonConverter());
    });

var authOptionsForStartup = builder.Configuration.GetSection(nameof(AuthOptions)).Get<AuthOptions>();
if (string.IsNullOrEmpty(authOptionsForStartup?.Secret))
{
    throw new InvalidOperationException($"AuthOptions.{nameof(AuthOptions.Secret)} must be provided in configuration.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtOptions =>
    {
        jwtOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOptionsForStartup.Issuer,
            ValidAudience = authOptionsForStartup.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptionsForStartup.Secret)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AuthConstants.UsernameClaim,
        };

        if (builder.Environment.EnvironmentName == "IntegrationTests")
        {
            jwtOptions.TokenValidationParameters.SignatureValidator = (token, _) => new JsonWebToken(token);
            jwtOptions.TokenValidationParameters.IssuerSigningKeyValidator = (_, _, _) => true;
        }
    })
    .AddScheme<ApiKeyAuthSchemeOptions, ApiKeyAuthHandler>(AuthConstants.ApiKey.Scheme, _ => { });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHealthChecks()
    .AddCheck("Self", () => HealthCheckResult.Healthy(), tags: ["api"]);

var app = builder.Build();

if (!builder.Environment.IsProduction())
{
    app.UseSwagger(options => options.RouteTemplate = "swagger/{documentName}/swagger.json");
    app.UseSwaggerUI(options =>
    {
        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        var apiDescriptionProvider = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        
        // Only show documents that have actual API endpoints
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            // Check if this version group has any API descriptions
            var groupItems = apiDescriptionProvider.ApiDescriptionGroups.Items
                .FirstOrDefault(g => g.GroupName == description.GroupName);
            
            // Only add endpoint if the group has items (non-empty)
            if (groupItems != null && groupItems.Items.Any())
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }
        }
				
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var securityContext = httpContext.RequestServices.GetRequiredService<ISecurityContext>();
				
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("HttpRequestClientHostIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
        diagnosticContext.Set("HttpRequestUrl", httpContext.Request.GetDisplayUrl());
        diagnosticContext.Set("UserId", securityContext.UserId?.ToString() ?? "Anonymous");
    };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(mainCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SecurityContextInitializerMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions()
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    })
    .AllowAnonymous();

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception while starting the application");
}
finally
{
    Log.Information("Shut down complete");
    await Log.CloseAndFlushAsync();
}

return;

void RegisterOptions(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<LoggingOptions>(configuration.GetSection("Logging"));
    services.Configure<AuthOptions>(configuration.GetSection(nameof(AuthOptions)));
}

void AddSecurityServices(IServiceCollection services)
{
    services.AddScoped<SecurityContext>();
    services.AddScoped<ISecurityContext>(sp => sp.GetRequiredService<SecurityContext>());
    services.AddScoped<SecurityContextFilter>();
}

void SetupLogging(IServiceCollection services, IConfiguration configuration)
{
    var loggingOptions = configuration.GetSection("Logging").Get<LoggingOptions>();
		
    services.AddSerilog((_, logger) =>
    {
        logger
            .Enrich.FromLogContext()
            .Enrich.WithMessageTemplate()
            .Enrich.WithCorrelationIdHeader("X-Correlation-ID")
            .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                .WithDefaultDestructurers()
                .WithDestructurers([new DbUpdateExceptionDestructurer()]))
            .Enrich.WithProperty("_Version", loggingOptions.Version)
            .WriteTo.Console(loggingOptions.ConsoleLogLevel);
			
        if (loggingOptions.Seq.Enabled)
        {
            logger.WriteTo.Seq(loggingOptions.Seq.ServerUrl, apiKey: loggingOptions.Seq.ApiKey);
        }
    });
}

public partial class Program
{
}
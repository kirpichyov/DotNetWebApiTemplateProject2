using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleProject.Application.Contracts;
using SampleProject.Application.Services;

namespace SampleProject.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IValidatorFactory, ValidatorFactory>();
        services.AddScoped<IHashingProvider, HashingProvider>();
        services.AddScoped<IAuthService, AuthService>();
        
        return services;
    }
}
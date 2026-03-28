using Microsoft.Extensions.Configuration;

namespace SampleProject.Api.Configuration;

// Ref: https://github.com/dotnet/aspnetcore/issues/37680#issuecomment-1331559463
public static class TestConfiguration
{
    private static readonly AsyncLocal<Action<IConfigurationBuilder>> Current = new();

    public static IConfigurationBuilder AddTestConfiguration(this IConfigurationBuilder configurationBuilder)
    {
        if (Current.Value is { } configure)
        {
            configure(configurationBuilder);
        }

        configurationBuilder.AddEnvironmentVariables();
        return configurationBuilder;
    }

    public static void Create(Action<IConfigurationBuilder> action)
    {
        Current.Value = action;
    }
}

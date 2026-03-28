using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleProject.DataAccess.Connection;
using SampleProject.DataAccess.Interceptors;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace SampleProject.IntegrationTests.Factory;

/// <summary>
/// PostgreSQL container is created in <see cref="InitializeAsync"/> (not the constructor) so Docker is not contacted
/// until the host is about to start — avoids failing fixture construction when Docker is down.
/// </summary>
public sealed class IntegrationTestsWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer _dbContainer;

    private readonly TestsDbMode _dbMode;
    private readonly string _realDbConnectionString;

    private WireMockServer _wireMockServer;

    public WireMockServer WireMockServer => _wireMockServer;

    public IntegrationTestsWebApplicationFactory(TestsDbMode dbMode, string realDbConnectionString)
    {
        _dbMode = dbMode;
        _realDbConnectionString = realDbConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(IntegrationTestsAppSettings.Settings);
        });

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DatabaseContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DatabaseContext>((sp, options) =>
            {
                var connectionString = _dbMode is TestsDbMode.RealDb
                    ? _realDbConnectionString
                    : _dbContainer.GetConnectionString();

                options.UseNpgsql(connectionString)
                    .AddInterceptors(new AuditableEntitySaveChangesInterceptor(sp))
                    .AddInterceptors(new SoftDeletableEntitySaveChangesInterceptor(sp))
                    .UseSnakeCaseNamingConvention();

                options.UseLoggerFactory(LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole()));
                options.EnableSensitiveDataLogging();
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        _wireMockServer = WireMockServer.Start();

        if (_dbMode is TestsDbMode.TestContainers)
        {
            _dbContainer = new PostgreSqlBuilder("postgres:18")
                .WithDatabase("sampleproject-int-tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithAutoRemove(true)
                .Build();

            await _dbContainer.StartAsync();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        _wireMockServer?.Stop();

        await base.DisposeAsync();

        if (_dbContainer is not null)
        {
            await _dbContainer.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Bogus;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleProject.Application.Constants;
using SampleProject.Application.Models.Auth;
using SampleProject.Application.Models.Users;
using SampleProject.Core.Models.Entities;
using SampleProject.DataAccess.Connection;
using SampleProject.IntegrationTests.Endpoints;
using SampleProject.IntegrationTests.Extensions;
using SampleProject.IntegrationTests.Factory;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;

namespace SampleProject.IntegrationTests.Fixture;

public sealed class SystemUnderTestFixture : IAsyncLifetime
{
    public const string DefaultTestPassword = "Testpass1!";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IntegrationTestsWebApplicationFactory _factory;
    private readonly Faker _faker = new();

    public RestEndpoints Endpoints { get; } = new();
    public string Username { get; private set; }
    public string Password { get; private set; } = DefaultTestPassword;
    public User TestUser { get; private set; }
    public JwtAuthResponse InitialAuth { get; private set; }

    public IntegrationTestsWebApplicationFactory Factory => _factory;

    public SystemUnderTestFixture()
    {
        var testsConfig = new ConfigurationBuilder()
            .AddJsonFile("integration-tests-config.json")
            .AddJsonFile("integration-tests-config.local.json", optional: true)
            .Build();

        var dbMode = testsConfig.GetSection("DbMode").Get<TestsDbMode?>() ?? TestsDbMode.TestContainers;
        var realDbConnectionString = testsConfig.GetConnectionString("RealDb") ?? string.Empty;

        _factory = new IntegrationTestsWebApplicationFactory(dbMode, realDbConnectionString);
    }

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await db.Database.MigrateAsync();
        }

        Username = _faker.Internet.UserName().Replace(" ", string.Empty);
        if (Username.Length > 40)
        {
            Username = Username[..40];
        }

        var anonymousClient = BuildAnonymousRestClient();

        var signUpRequest = Endpoints.AuthSignUp(new SignUpRequest
        {
            Username = Username,
            FullName = _faker.Name.FullName(),
            Password = Password,
        });
        var signUpResponse = await anonymousClient.ExecuteAsync(signUpRequest, TestContext.Current.CancellationToken);
        signUpResponse.ThrowOnFailStatusCode();

        var signInRequest = Endpoints.AuthSignIn(new SignInRequest
        {
            Username = Username,
            Password = Password,
            AuthType = AuthTypeModel.AccessTokenWithRefreshToken,
        });
        var signInResponse = await anonymousClient.ExecuteAsync(signInRequest, TestContext.Current.CancellationToken);
        signInResponse.ThrowOnFailStatusCode();
        InitialAuth = signInResponse.DeserializeData<JwtAuthResponse>();
        ArgumentNullException.ThrowIfNull(InitialAuth);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            TestUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == InitialAuth.UserId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    public IServiceScope CreateScope()
    {
        return _factory.Services.CreateScope();
    }

    public HttpClient GetHttpClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    public RestClient BuildAnonymousRestClient()
    {
        return BuildRestClient(token: null);
    }

    public RestClient BuildRestClient(string token)
    {
        IAuthenticator authenticator = null;
        if (!string.IsNullOrEmpty(token))
        {
            authenticator = new JwtAuthenticator(token);
        }

        var httpClient = GetHttpClient();
        return new RestClient(
            httpClient,
            new RestClientOptions
            {
                Authenticator = authenticator,
            },
            configureSerialization: cfg => cfg.UseSystemTextJson(JsonSerializerOptions));
    }

    public RestClient BuildRestClientForApiKey(string fullApiKey)
    {
        var httpClient = GetHttpClient();
        return new RestClient(
            httpClient,
            new RestClientOptions
            {
                Authenticator = new ApiKeyAuthenticator(fullApiKey),
            },
            configureSerialization: cfg => cfg.UseSystemTextJson(JsonSerializerOptions));
    }

    public UserWithRestClient CreatePrimaryUserContext()
    {
        var client = BuildRestClient(InitialAuth.AccessToken.Token);
        return new UserWithRestClient
        {
            User = TestUser,
            RestClient = client,
        };
    }

    private sealed class ApiKeyAuthenticator(string apiKey) : IAuthenticator
    {
        public ValueTask Authenticate(IRestClient client, RestRequest request, CancellationToken cancellationToken)
        {
            request.AddOrUpdateHeader("Authorization", $"{AuthConstants.ApiKey.Scheme} {apiKey}");
            return ValueTask.CompletedTask;
        }
    }
}

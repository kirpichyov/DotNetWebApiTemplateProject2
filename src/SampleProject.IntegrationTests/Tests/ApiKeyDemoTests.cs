using FluentAssertions;
using SampleProject.Application.Models.ApiKeys;
using SampleProject.Application.Models.Users;
using SampleProject.IntegrationTests.Endpoints;
using SampleProject.IntegrationTests.Extensions;
using SampleProject.IntegrationTests.Fixture;
using RestSharp;
using System.Net;

namespace SampleProject.IntegrationTests.Tests;

public sealed class ApiKeyDemoTests : IClassFixture<SystemUnderTestFixture>
{
    private readonly SystemUnderTestFixture _sut;

    public ApiKeyDemoTests(SystemUnderTestFixture sut)
    {
        _sut = sut;
    }

    [Fact]
    public async Task Me_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _sut.BuildAnonymousRestClient();
        var response = await client.ExecuteAsync(
            _sut.Endpoints.ApiKeyDemoMe(),
            TestContext.Current.CancellationToken);

        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithBearer_ReturnsUnauthorized()
    {
        var ctx = _sut.CreatePrimaryUserContext();
        var response = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.ApiKeyDemoMe(),
            TestContext.Current.CancellationToken);

        response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithApiKeyHeader_ReturnsSameUserAsJwtMe()
    {
        var ctx = _sut.CreatePrimaryUserContext();
        var createResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysCreate(new CreateUserApiKeyRequest { Name = "demo-key" }),
            TestContext.Current.CancellationToken);

        createResponse.ThrowOnFailStatusCode();
        var created = createResponse.DeserializeData<CreateUserApiKeyResponse>();
        created.Should().NotBeNull();

        var apiKeyClient = _sut.BuildRestClientForApiKey(created!.FullKey);
        var demoResponse = await apiKeyClient.ExecuteAsync(
            _sut.Endpoints.ApiKeyDemoMe(),
            TestContext.Current.CancellationToken);

        demoResponse.Should().HaveStatusCode(HttpStatusCode.OK);
        var demoUser = demoResponse.DeserializeData<CurrentUserDataResponse>();
        demoUser!.Id.Should().Be(ctx.User.Id);
        demoUser.Username.Should().Be(ctx.User.Username);

        var jwtMeResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.AuthMe(),
            TestContext.Current.CancellationToken);

        jwtMeResponse.ThrowOnFailStatusCode();
        var jwtUser = jwtMeResponse.DeserializeData<CurrentUserDataResponse>();
        jwtUser!.Id.Should().Be(demoUser.Id);
    }

    [Fact]
    public async Task Me_WithApiKeyQuery_ReturnsOk()
    {
        var ctx = _sut.CreatePrimaryUserContext();
        var createResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysCreate(new CreateUserApiKeyRequest { Name = "query-key" }),
            TestContext.Current.CancellationToken);

        createResponse.ThrowOnFailStatusCode();
        var created = createResponse.DeserializeData<CreateUserApiKeyResponse>();
        created.Should().NotBeNull();

        var fullKey = Uri.EscapeDataString(created!.FullKey);
        var request = new RestRequest($"{EndpointPaths.ApiKeyDemoMe}?apiKey={fullKey}", Method.Get);
        var client = _sut.BuildAnonymousRestClient();
        var response = await client.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var user = response.DeserializeData<CurrentUserDataResponse>();
        user!.Id.Should().Be(ctx.User.Id);
    }
}

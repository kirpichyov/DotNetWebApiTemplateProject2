using SampleProject.Application.Models.ApiKeys;
using SampleProject.Application.Models.Auth;
using SampleProject.IntegrationTests.Fixture;
using System.Net;

namespace SampleProject.IntegrationTests.Tests;

public sealed class UserApiKeysTests : IClassFixture<SystemUnderTestFixture>
{
    private readonly SystemUnderTestFixture _sut;

    public UserApiKeysTests(SystemUnderTestFixture sut)
    {
        _sut = sut;
    }

    [Fact]
    public async Task List_WhenEmpty_ReturnsOkAndEmptyCollection()
    {
        // Dedicated user: IClassFixture shares one primary user across this class; other tests create keys on it.
        var faker = new Bogus.Faker();
        var rawUsername = faker.Internet.UserName().Replace(" ", string.Empty);
        var username = rawUsername.Length > 35 ? rawUsername[..35] : rawUsername;
        var anonymous = _sut.BuildAnonymousRestClient();

        await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthSignUp(new SignUpRequest
            {
                Username = username,
                FullName = faker.Name.FullName(),
                Password = SystemUnderTestFixture.DefaultTestPassword,
            }),
            TestContext.Current.CancellationToken).ThrowOnFailStatusCode();

        var signIn = await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthSignIn(new SignInRequest
            {
                Username = username,
                Password = SystemUnderTestFixture.DefaultTestPassword,
                AuthType = AuthTypeModel.AccessTokenOnly,
            }),
            TestContext.Current.CancellationToken);

        signIn.ThrowOnFailStatusCode();
        var auth = signIn.DeserializeData<JwtAuthResponse>();
        auth.ShouldNotBeNull();

        var client = _sut.BuildRestClient(auth!.AccessToken.Token);
        var response = await client.ExecuteAsync(
            _sut.Endpoints.UserApiKeysList(),
            TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var data = response.DeserializeData<List<UserApiKeyResponse>>();
        data.ShouldNotBeNull();
        data!.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_Get_List_Update_Rotate_Revoke_Flow()
    {
        var ctx = _sut.CreatePrimaryUserContext();

        var createResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysCreate(new CreateUserApiKeyRequest { Name = "integration-key" }),
            TestContext.Current.CancellationToken);

        createResponse.ShouldHaveStatusCode(HttpStatusCode.Created);
        var created = createResponse.DeserializeData<CreateUserApiKeyResponse>();
        created.ShouldNotBeNull();
        created!.FullKey.ShouldNotBeNullOrWhiteSpace();
        created.Key.Name.ShouldBe("integration-key");

        var keyId = created.Key.Id;

        var getResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysGet(keyId),
            TestContext.Current.CancellationToken);

        getResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var got = getResponse.DeserializeData<UserApiKeyResponse>();
        got!.Id.ShouldBe(keyId);

        var listResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysList(),
            TestContext.Current.CancellationToken);

        listResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var list = listResponse.DeserializeData<List<UserApiKeyResponse>>();
        list.ShouldContain(k => k.Id == keyId);

        var updateResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysUpdate(keyId, new UpdateUserApiKeyRequest { Name = "renamed" }),
            TestContext.Current.CancellationToken);

        updateResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var updated = updateResponse.DeserializeData<UserApiKeyResponse>();
        updated!.Name.ShouldBe("renamed");

        var rotateResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysRotate(keyId),
            TestContext.Current.CancellationToken);

        rotateResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var rotated = rotateResponse.DeserializeData<CreateUserApiKeyResponse>();
        rotated!.FullKey.ShouldNotBeNullOrWhiteSpace();
        rotated.FullKey.ShouldNotBe(created.FullKey);

        var revokeResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysRevoke(keyId),
            TestContext.Current.CancellationToken);

        revokeResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Backend soft-revokes: row remains; GET returns 200 with isActive false (see UserApiKeysService.GetByIdAsync).
        var getAfterRevoke = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysGet(keyId),
            TestContext.Current.CancellationToken);

        getAfterRevoke.ShouldHaveStatusCode(HttpStatusCode.OK);
        var revoked = getAfterRevoke.DeserializeData<UserApiKeyResponse>();
        revoked.ShouldNotBeNull();
        revoked!.Id.ShouldBe(keyId);
        revoked.IsActive.ShouldBeFalse();
        revoked.RevokedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_WithApiKeyScheme_ReturnsUnauthorized()
    {
        var ctx = _sut.CreatePrimaryUserContext();
        var createFirstResponse = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysCreate(new CreateUserApiKeyRequest { Name = "for-api-key-client" }),
            TestContext.Current.CancellationToken);

        createFirstResponse.ThrowOnFailStatusCode();
        var createFirst = createFirstResponse.DeserializeData<CreateUserApiKeyResponse>();
        createFirst.ShouldNotBeNull();

        var apiKeyClient = _sut.BuildRestClientForApiKey(createFirst!.FullKey);
        var response = await apiKeyClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysCreate(new CreateUserApiKeyRequest { Name = "should-fail" }),
            TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_AnotherUsersKey_ReturnsNotFound()
    {
        var owner = _sut.CreatePrimaryUserContext();
        var createResponse = await owner.RestClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysCreate(new CreateUserApiKeyRequest { Name = "owner-key" }),
            TestContext.Current.CancellationToken);

        createResponse.ThrowOnFailStatusCode();
        var created = createResponse.DeserializeData<CreateUserApiKeyResponse>();
        created.ShouldNotBeNull();
        var keyId = created!.Key.Id;

        var faker = new Bogus.Faker();
        var rawUsername = faker.Internet.UserName().Replace(" ", string.Empty);
        var otherUsername = rawUsername.Length > 40 ? rawUsername[..40] : rawUsername;
        var anonymous = _sut.BuildAnonymousRestClient();

        var signUpOther = await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthSignUp(new SignUpRequest
            {
                Username = otherUsername,
                FullName = faker.Name.FullName(),
                Password = SystemUnderTestFixture.DefaultTestPassword,
            }),
            TestContext.Current.CancellationToken);

        signUpOther.ThrowOnFailStatusCode();

        var otherSignInResponse = await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthSignIn(new SignInRequest
            {
                Username = otherUsername,
                Password = SystemUnderTestFixture.DefaultTestPassword,
                AuthType = AuthTypeModel.AccessTokenOnly,
            }),
            TestContext.Current.CancellationToken);

        otherSignInResponse.ThrowOnFailStatusCode();
        var otherAuth = otherSignInResponse.DeserializeData<JwtAuthResponse>();
        otherAuth.ShouldNotBeNull();

        var otherClient = _sut.BuildRestClient(otherAuth!.AccessToken.Token);
        var response = await otherClient.ExecuteAsync(
            _sut.Endpoints.UserApiKeysGet(keyId),
            TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }
}

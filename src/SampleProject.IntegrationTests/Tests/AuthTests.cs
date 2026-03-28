using SampleProject.Application.Models.Auth;
using SampleProject.Application.Models.Users;
using SampleProject.IntegrationTests.Fixture;
using System.Net;

namespace SampleProject.IntegrationTests.Tests;

public sealed class AuthTests : IClassFixture<SystemUnderTestFixture>
{
    private readonly SystemUnderTestFixture _sut;

    public AuthTests(SystemUnderTestFixture sut)
    {
        _sut = sut;
    }

    [Fact]
    public async Task SignUp_DuplicateUsername_ReturnsBadRequest()
    {
        var client = _sut.BuildAnonymousRestClient();
        var response = await client.ExecuteAsync(
            _sut.Endpoints.AuthSignUp(new SignUpRequest
            {
                Username = _sut.Username,
                FullName = "Someone Else",
                Password = SystemUnderTestFixture.DefaultTestPassword,
            }),
            TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignIn_WrongPassword_ReturnsBadRequest()
    {
        var client = _sut.BuildAnonymousRestClient();
        var response = await client.ExecuteAsync(
            _sut.Endpoints.AuthSignIn(new SignInRequest
            {
                Username = _sut.Username,
                Password = "Wrongpass1!",
                AuthType = AuthTypeModel.AccessTokenWithRefreshToken,
            }),
            TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var client = _sut.BuildAnonymousRestClient();
        var response = await client.ExecuteAsync(
            _sut.Endpoints.AuthMe(),
            TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithJwt_ReturnsOkWithCurrentUser()
    {
        var ctx = _sut.CreatePrimaryUserContext();
        var response = await ctx.RestClient.ExecuteAsync(
            _sut.Endpoints.AuthMe(),
            TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var data = response.DeserializeData<CurrentUserDataResponse>();
        data.ShouldNotBeNull();
        data!.Id.ShouldBe(ctx.User.Id);
        data.Username.ShouldBe(ctx.User.Username);
    }

    [Fact]
    public async Task Refresh_ValidPair_ReturnsNewTokens()
    {
        var client = _sut.BuildAnonymousRestClient();
        var authResponse = await client.ExecuteAsync(
            _sut.Endpoints.AuthSignIn(new SignInRequest
            {
                Username = _sut.Username,
                Password = _sut.Password,
                AuthType = AuthTypeModel.AccessTokenWithRefreshToken,
            }),
            TestContext.Current.CancellationToken);

        authResponse.ThrowOnFailStatusCode();
        var auth = authResponse.DeserializeData<JwtAuthResponse>();
        auth.ShouldNotBeNull();
        auth!.RefreshToken.ShouldNotBeNull();

        var refreshResponse = await client.ExecuteAsync(
            _sut.Endpoints.AuthRefresh(new RefreshAccessTokenRequest
            {
                AccessToken = auth.AccessToken.Token,
                RefreshToken = auth.RefreshToken!.Token,
            }),
            TestContext.Current.CancellationToken);

        refreshResponse.ShouldHaveStatusCode(HttpStatusCode.OK);
        var refreshed = refreshResponse.DeserializeData<JwtAuthResponse>();
        refreshed.ShouldNotBeNull();
        refreshed!.AccessToken.Token.ShouldNotBeNullOrEmpty();
        refreshed.AccessToken.Token.ShouldNotBe(auth.AccessToken.Token);
    }

    [Fact]
    public async Task Refresh_AfterRefreshWithOldPair_ReturnsBadRequest()
    {
        var client = _sut.BuildAnonymousRestClient();
        var authResponse = await client.ExecuteAsync(
            _sut.Endpoints.AuthSignIn(new SignInRequest
            {
                Username = _sut.Username,
                Password = _sut.Password,
                AuthType = AuthTypeModel.AccessTokenWithRefreshToken,
            }),
            TestContext.Current.CancellationToken);

        authResponse.ThrowOnFailStatusCode();
        var auth = authResponse.DeserializeData<JwtAuthResponse>();
        auth.ShouldNotBeNull();

        var refreshedResponse = await client.ExecuteAsync(
            _sut.Endpoints.AuthRefresh(new RefreshAccessTokenRequest
            {
                AccessToken = auth!.AccessToken.Token,
                RefreshToken = auth.RefreshToken!.Token,
            }),
            TestContext.Current.CancellationToken);

        refreshedResponse.ThrowOnFailStatusCode();

        var secondWithOldPair = await client.ExecuteAsync(
            _sut.Endpoints.AuthRefresh(new RefreshAccessTokenRequest
            {
                AccessToken = auth.AccessToken.Token,
                RefreshToken = auth.RefreshToken.Token,
            }),
            TestContext.Current.CancellationToken);

        secondWithOldPair.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_ThenSignIn_UsesNewPassword()
    {
        const string newPassword = "Newpass1!";
        var faker = new Bogus.Faker();
        var rawUsername = faker.Internet.UserName().Replace(" ", string.Empty);
        var username = rawUsername.Length > 30 ? rawUsername[..30] : rawUsername;
        var anonymous = _sut.BuildAnonymousRestClient();

        var signUpResponse = await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthSignUp(new SignUpRequest
            {
                Username = username,
                FullName = faker.Name.FullName(),
                Password = SystemUnderTestFixture.DefaultTestPassword,
            }),
            TestContext.Current.CancellationToken);

        signUpResponse.ThrowOnFailStatusCode();

        var signedInResponse = await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthSignIn(new SignInRequest
            {
                Username = username,
                Password = SystemUnderTestFixture.DefaultTestPassword,
                AuthType = AuthTypeModel.AccessTokenWithRefreshToken,
            }),
            TestContext.Current.CancellationToken);

        signedInResponse.ThrowOnFailStatusCode();
        var signedIn = signedInResponse.DeserializeData<JwtAuthResponse>();
        signedIn.ShouldNotBeNull();

        var authenticated = _sut.BuildRestClient(signedIn!.AccessToken.Token);
        var changeResponse = await authenticated.ExecuteAsync(
            _sut.Endpoints.AuthChangePassword(new ChangePasswordRequest
            {
                CurrentPassword = SystemUnderTestFixture.DefaultTestPassword,
                NewPassword = newPassword,
                ExpireAllSessions = false,
            }),
            TestContext.Current.CancellationToken);

        changeResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var oldPasswordSignIn = await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthSignIn(new SignInRequest
            {
                Username = username,
                Password = SystemUnderTestFixture.DefaultTestPassword,
                AuthType = AuthTypeModel.AccessTokenOnly,
            }),
            TestContext.Current.CancellationToken);

        oldPasswordSignIn.ShouldHaveStatusCode(HttpStatusCode.BadRequest);

        var newPasswordSignIn = await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthSignIn(new SignInRequest
            {
                Username = username,
                Password = newPassword,
                AuthType = AuthTypeModel.AccessTokenOnly,
            }),
            TestContext.Current.CancellationToken);

        newPasswordSignIn.ShouldHaveStatusCode(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivateRefresh_ThenRefreshWithSamePair_ReturnsBadRequest()
    {
        var anonymous = _sut.BuildAnonymousRestClient();
        var authResponse = await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthSignIn(new SignInRequest
            {
                Username = _sut.Username,
                Password = _sut.Password,
                AuthType = AuthTypeModel.AccessTokenWithRefreshToken,
            }),
            TestContext.Current.CancellationToken);

        authResponse.ThrowOnFailStatusCode();
        var auth = authResponse.DeserializeData<JwtAuthResponse>();
        auth.ShouldNotBeNull();

        var jwtClient = _sut.BuildRestClient(auth!.AccessToken.Token);
        var deactivateResponse = await jwtClient.ExecuteAsync(
            _sut.Endpoints.AuthDeactivateRefresh(new ExpireRefreshTokenRequest
            {
                AccessToken = auth.AccessToken.Token,
                RefreshToken = auth.RefreshToken!.Token,
            }),
            TestContext.Current.CancellationToken);

        deactivateResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        var refreshAfter = await anonymous.ExecuteAsync(
            _sut.Endpoints.AuthRefresh(new RefreshAccessTokenRequest
            {
                AccessToken = auth.AccessToken.Token,
                RefreshToken = auth.RefreshToken.Token,
            }),
            TestContext.Current.CancellationToken);

        refreshAfter.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }
}

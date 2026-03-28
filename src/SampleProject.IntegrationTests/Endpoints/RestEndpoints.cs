using SampleProject.Application.Models.ApiKeys;
using SampleProject.Application.Models.Auth;
using SampleProject.Application.Models.Users;
using RestSharp;

namespace SampleProject.IntegrationTests.Endpoints;

public sealed class RestEndpoints
{
    public RestRequest AuthSignUp(SignUpRequest body) =>
        new RestRequest(EndpointPaths.AuthSignUp, Method.Post).AddJsonBody(body);

    public RestRequest AuthSignIn(SignInRequest body) =>
        new RestRequest(EndpointPaths.AuthSignIn, Method.Post).AddJsonBody(body);

    public RestRequest AuthMe() =>
        new RestRequest(EndpointPaths.AuthMe, Method.Get);

    public RestRequest AuthRefresh(RefreshAccessTokenRequest body) =>
        new RestRequest(EndpointPaths.AuthRefresh, Method.Post).AddJsonBody(body);

    public RestRequest AuthChangePassword(ChangePasswordRequest body) =>
        new RestRequest(EndpointPaths.AuthChangePassword, Method.Post).AddJsonBody(body);

    public RestRequest AuthDeactivateRefresh(ExpireRefreshTokenRequest body) =>
        new RestRequest(EndpointPaths.AuthDeactivateRefresh, Method.Post).AddJsonBody(body);

    public RestRequest UserApiKeysList() =>
        new RestRequest(EndpointPaths.UserApiKeys, Method.Get);

    public RestRequest UserApiKeysCreate(CreateUserApiKeyRequest body) =>
        new RestRequest(EndpointPaths.UserApiKeys, Method.Post).AddJsonBody(body);

    public RestRequest UserApiKeysGet(Guid keyId) =>
        new RestRequest(EndpointPaths.UserApiKeyById(keyId), Method.Get);

    public RestRequest UserApiKeysUpdate(Guid keyId, UpdateUserApiKeyRequest body) =>
        new RestRequest(EndpointPaths.UserApiKeyById(keyId), Method.Put).AddJsonBody(body);

    public RestRequest UserApiKeysRevoke(Guid keyId) =>
        new RestRequest(EndpointPaths.UserApiKeyById(keyId), Method.Delete);

    public RestRequest UserApiKeysRotate(Guid keyId) =>
        new RestRequest(EndpointPaths.UserApiKeyRotate(keyId), Method.Post);

    public RestRequest ApiKeyDemoMe() =>
        new RestRequest(EndpointPaths.ApiKeyDemoMe, Method.Get);
}

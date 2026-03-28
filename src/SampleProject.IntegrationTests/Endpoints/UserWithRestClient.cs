using SampleProject.Core.Models.Entities;
using RestSharp;

namespace SampleProject.IntegrationTests.Endpoints;

public sealed class UserWithRestClient
{
    public User User { get; init; }
    public RestClient RestClient { get; init; }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using RestSharp;

namespace SampleProject.IntegrationTests.Extensions;

internal static class RestResponseExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static T DeserializeData<T>(this RestResponse response)
    {
        if (response.Content is not { Length: > 0 })
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(response.Content, JsonOptions);
    }

    public static void ThrowOnFailStatusCode(this RestResponse response)
    {
        var code = (int)response.StatusCode;
        if (code is >= 200 and <= 299)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected to receive success result from {response.Request.Method} {response.Request.Resource}. " +
            $"But actual status code was {response.StatusCode} with content: {response.Content}");
    }

    public static async Task ThrowOnFailStatusCode(this Task<RestResponse> response)
    {
        var restResponse = await response;
        restResponse.ThrowOnFailStatusCode();
    }
}

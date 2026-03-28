using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using RestSharp;
using System.Net;

namespace SampleProject.IntegrationTests.Extensions;

internal static class RestResponseAssertionsExtensions
{
    public static RestResponseAssertions Should(this RestResponse response)
        => new(response, AssertionChain.GetOrCreate());
}

internal sealed class RestResponseAssertions(RestResponse subject, AssertionChain assertionChain)
    : ReferenceTypeAssertions<RestResponse, RestResponseAssertions>(subject, assertionChain)
{
    protected override string Identifier => "response";

    public AndConstraint<RestResponseAssertions> HaveStatusCode(
        HttpStatusCode expected,
        string because = "",
        params object[] becauseArgs)
    {
        CurrentAssertionChain
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.StatusCode == expected)
            .FailWith(
                "Expected {context:response} from {0} {1} to have status code {2}{reason}, but found {3}.{4}",
                Subject.Request?.Method.ToString() ?? "?",
                Subject.Request?.Resource ?? "?",
                expected,
                Subject.StatusCode,
                Subject.Content is { Length: > 0 }
                    ? $"\nResponse body:\n{Subject.Content}"
                    : string.Empty);

        return new AndConstraint<RestResponseAssertions>(this);
    }
}

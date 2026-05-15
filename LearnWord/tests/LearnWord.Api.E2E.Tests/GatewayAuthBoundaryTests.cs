using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace LearnWord.Api.E2E.Tests;

public class GatewayAuthBoundaryTests
{
    [Theory]
    [E2EProtectedRouteData]
    public async Task ProtectedGatewayRoutes_WithoutBearerToken_ReturnUnauthorized(HttpRequestMessage request)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("LEARNWORD_GATEWAY_URL") ?? "http://localhost:5100")
        };

        using var response = await httpClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class E2EProtectedRouteDataAttribute : DataAttribute
    {
        public E2EProtectedRouteDataAttribute()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("LEARNWORD_E2E"), "true", StringComparison.OrdinalIgnoreCase))
            {
                Skip = "Set LEARNWORD_E2E=true and run local Docker to execute gateway E2E tests.";
            }
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            yield return [new HttpRequestMessage(HttpMethod.Get, "/api/collections")];
            yield return [new HttpRequestMessage(HttpMethod.Post, "/api/cards")
            {
                Content = JsonContent.Create(new { collectionId = 1, words = Array.Empty<object>() })
            }];
            yield return [new HttpRequestMessage(HttpMethod.Post, "/api/cards/1/words")
            {
                Content = JsonContent.Create(new { value = "cat", transcription = "kat", translation = "cat" })
            }];
        }
    }
}

using System.Net;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Options;
using LearnWord.WebApi.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace LearnWord.WebApi.Tests;

public class ConfiguredAiCardGenerationProviderTests
{
    [Fact]
    public async Task GenerateCards_OpenRouterWithoutApiKey_UsesFakeProviderWithoutNetworkCall()
    {
        var httpClient = new HttpClient(new ThrowingHttpMessageHandler());
        var options = OptionsFactory.Create(new AiCardGenerationOptions
        {
            Provider = "OpenRouter",
            OpenRouter = new OpenRouterOptions
            {
                ApiKey = "",
                BaseUrl = "https://openrouter.ai/api/v1/chat/completions"
            }
        });
        var openRouterProvider = new OpenRouterAiCardGenerationProvider(
            httpClient,
            options,
            new NoopLogger<OpenRouterAiCardGenerationProvider>());
        var provider = new ConfiguredAiCardGenerationProvider(
            new FakeAiCardGenerationProvider(),
            openRouterProvider,
            options,
            new NoopLogger<ConfiguredAiCardGenerationProvider>());

        var result = await provider.GenerateCards(
            new AiCardGenerationRequest
            {
                SourceText = "alpha beta",
                TargetLanguage = "Spanish",
                MaxCards = 1
            },
            CancellationToken.None);

        var card = Assert.Single(result.Cards);
        Assert.Equal("alpha", card.Value);
        Assert.Equal("[Spanish] alpha", card.Translation);
    }

    [Fact]
    public async Task GenerateCards_OpenRouterRateLimit_ThrowsUserFacingRateLimitError()
    {
        var httpClient = new HttpClient(new StaticHttpMessageHandler(
            HttpStatusCode.TooManyRequests,
            """{"error":{"message":"rate limited"}}"""));
        var options = OptionsFactory.Create(new AiCardGenerationOptions
        {
            Provider = "OpenRouter",
            OpenRouter = new OpenRouterOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://openrouter.ai/api/v1/chat/completions"
            }
        });
        var provider = new OpenRouterAiCardGenerationProvider(
            httpClient,
            options,
            new NoopLogger<OpenRouterAiCardGenerationProvider>());

        var exception = await Assert.ThrowsAsync<UpstreamServiceException>(() =>
            provider.GenerateCards(
                new AiCardGenerationRequest { SourceText = "alpha beta", MaxCards = 1 },
                CancellationToken.None));

        Assert.Equal(429, exception.StatusCode);
        Assert.Equal("ai_provider_rate_limited", exception.ErrorCode);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("OpenRouter network call should not be used without an API key.");
        }
    }

    private sealed class StaticHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;
        private readonly string body;

        public StaticHttpMessageHandler(HttpStatusCode statusCode, string body)
        {
            this.statusCode = statusCode;
            this.body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body)
            });
        }
    }

    private sealed class NoopLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.BL.Services;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using LearnWord.WebApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LearnWord.WebApi.Tests;

public class CoachConfigurationIsolationTests
{
    private const string None = """{"decision":"None","summary":"","observations":[],"interventions":[]}""";
    private static readonly AiCardGenerationRequest CardRequest = new() { SourceText = "alpha", MaxCards = 1 };

    [Fact]
    public void DefaultsAreSafeAndOpenRouterSettingsAreIndependentInstances()
    {
        var coach = new AiCoachOptions(); var cards = new AiCardGenerationOptions();
        Assert.Equal("Fake", coach.Provider);
        Assert.Equal("z-ai/glm-5.3-flash", coach.OpenRouter.Model);
        Assert.Equal("low", coach.ReasoningEffort); Assert.Equal(3000, coach.MaxOutputTokens);
        Assert.Equal("google/gemma-4-26b-a4b-it:free", cards.OpenRouter.Model);
        Assert.NotSame(coach.OpenRouter, cards.OpenRouter);
        coach.OpenRouter.Model = "changed-coach";
        Assert.Equal("google/gemma-4-26b-a4b-it:free", cards.OpenRouter.Model);
    }

    [Fact]
    public async Task BoundTypedProvidersSendGlmCoachAndGemmaCardsWithOwnKeysAndUrls()
    {
        using var fixture = Create(Settings());
        var coach = fixture.Services.GetRequiredService<IAiCoachProvider>();
        var cards = fixture.Services.GetRequiredService<IAiCardGenerationProvider>();
        await coach.AnalyzeWithDiagnostics(CoachOutputValidatorTests.Context(), default);
        await cards.GenerateCards(CardRequest, default);
        Assert.Equal("OpenRouter", coach.Provider); Assert.Equal("z-ai/glm-5.3-flash", coach.Model);
        var body = JsonNode.Parse(fixture.Coach.Body!)!;
        Assert.Equal("z-ai/glm-5.3-flash", body["model"]!.GetValue<string>());
        Assert.Equal("low", body["reasoning"]!["effort"]!.GetValue<string>());
        Assert.Equal(3000, body["max_tokens"]!.GetValue<int>());
        Assert.True(body["provider"]!["require_parameters"]!.GetValue<bool>());
        Assert.Equal("https://coach.example.test/chat", fixture.Coach.Url);
        Assert.Equal("Bearer PRIVATE_COACH_KEY", fixture.Coach.Authorization);
        var cardBody = JsonNode.Parse(fixture.Cards.Body!)!;
        Assert.Equal("google/gemma-4-26b-a4b-it", cardBody["model"]!.GetValue<string>());
        Assert.Null(cardBody["reasoning"]); Assert.Null(cardBody["provider"]);
        Assert.Equal("https://cards.example.test/chat", fixture.Cards.Url);
        Assert.Equal("Bearer PRIVATE_CARD_KEY", fixture.Cards.Authorization);
        Assert.DoesNotContain("PRIVATE_", fixture.Coach.Body); Assert.DoesNotContain("PRIVATE_", fixture.Cards.Body);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangingEitherModelsSectionDoesNotChangeOtherProvidersRequest(bool changeCoach)
    {
        using var first = Create(Settings());
        await CallBoth(first);
        var changed = Settings();
        changed[changeCoach ? "AiCoach:OpenRouter:Model" : "AiCardGeneration:OpenRouter:Model"] = "changed-model";
        if (changeCoach) { changed["AiCoach:ReasoningEffort"] = "high"; changed["AiCoach:MaxOutputTokens"] = "5000"; }
        using var second = Create(changed);
        await CallBoth(second);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(changeCoach ? first.Cards.Body! : first.Coach.Body!),
            JsonNode.Parse(changeCoach ? second.Cards.Body! : second.Coach.Body!)));
    }

    [Fact]
    public async Task MissingCoachKeyNeverFallsBackToCardKeyAndCardGeneratorStillWorks()
    {
        var settings = Settings(); settings.Remove("AiCoach:OpenRouter:ApiKey");
        using var fixture = Create(settings);
        var error = await Assert.ThrowsAsync<UpstreamServiceException>(() => fixture.Services.GetRequiredService<IAiCoachProvider>()
            .Analyze(CoachOutputValidatorTests.Context(), default));
        Assert.Equal("ai_provider_configuration_error", error.ErrorCode);
        Assert.Equal("Configuration", CoachFailure.Details(error)!.FailureStage);
        Assert.Equal(0, fixture.Coach.Calls);
        await fixture.Services.GetRequiredService<IAiCardGenerationProvider>().GenerateCards(CardRequest, default);
        Assert.Equal(1, fixture.Cards.Calls);
    }

    [Theory]
    [InlineData("AiCoach:MaxOutputTokens", "0")]
    [InlineData("AiCoach:MaxOutputTokens", "5001")]
    [InlineData("AiCoach:ReasoningEffort", "none")]
    [InlineData("AiCoach:ReasoningEffort", "PRIVATE_UNSUPPORTED")]
    [InlineData("AiCoach:OpenRouter:BaseUrl", "PRIVATE_INVALID_URL")]
    [InlineData("AiCoach:OpenRouter:ApiKey", "PRIVATE_KEY\r\n")]
    [InlineData("AiCoach:OpenRouter:Model", "")]
    public async Task InvalidCoachConfigurationIsNoncriticalAndDoesNotAffectCards(string field, string value)
    {
        var settings = Settings(); settings[field] = value;
        using var fixture = Create(settings);
        var error = await Assert.ThrowsAsync<UpstreamServiceException>(() => fixture.Services.GetRequiredService<IAiCoachProvider>()
            .Analyze(CoachOutputValidatorTests.Context(), default));
        Assert.Equal("ai_provider_configuration_error", error.ErrorCode);
        Assert.Equal("Configuration", CoachFailure.Details(error)!.FailureStage);
        Assert.DoesNotContain("PRIVATE_", error.Message); Assert.Equal(0, fixture.Coach.Calls);
        await fixture.Services.GetRequiredService<IAiCardGenerationProvider>().GenerateCards(CardRequest, default);
        Assert.Equal(1, fixture.Cards.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProviderSelectionIsIndependentForCoachAndCards(bool fakeCoach)
    {
        var settings = Settings(); settings[fakeCoach ? "AiCoach:Provider" : "AiCardGeneration:Provider"] = "Fake";
        using var fixture = Create(settings);
        await CallBoth(fixture);
        Assert.Equal(fakeCoach ? 0 : 1, fixture.Coach.Calls);
        Assert.Equal(fakeCoach ? 1 : 0, fixture.Cards.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ExplicitUnspecifiedCoachEffortOmitsReasoningProperty(string? effort)
    {
        var settings = Settings();
        if (effort != null) settings["AiCoach:ReasoningEffort"] = effort;
        using var fixture = Create(settings);
        if (effort == null) fixture.Services.GetRequiredService<IOptions<AiCoachOptions>>().Value.ReasoningEffort = null;
        await fixture.Services.GetRequiredService<IAiCoachProvider>().Analyze(CoachOutputValidatorTests.Context(), default);
        Assert.False(JsonNode.Parse(fixture.Coach.Body!)!.AsObject().ContainsKey("reasoning"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ServicePreservesGlmProvenanceAndDisabledFeatureNeverCallsProvider(bool enabled)
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var settings = Settings(); settings["AiCoach:Enabled"] = enabled.ToString();
        using var fixture = Create(settings);
        var options = fixture.Services.GetRequiredService<IOptions<AiCoachOptions>>();
        var service = new CoachService(db.Context, new CoachContextBuilder(db.Context, new LearnerStateCalculator()),
            fixture.Services.GetRequiredService<IAiCoachProvider>(), options, NullLogger<CoachService>.Instance);
        var result = await service.Analyze(session.Id, new());
        Assert.Equal(enabled ? "Completed" : "Disabled", result.Status);
        Assert.Equal(enabled ? 1 : 0, fixture.Coach.Calls);
        if (enabled)
        {
            Assert.Equal("OpenRouter", result.Provider); Assert.Equal("z-ai/glm-5.3-flash", result.Model);
            Assert.Equal("coach-v6", result.PromptVersion);
            var diagnostic = await service.Diagnostics(session.Id);
            Assert.Equal(result.Provider, diagnostic.Analysis.Provider); Assert.Equal(result.Model, diagnostic.Analysis.Model);
        }
    }

    private static async Task CallBoth(Fixture fixture)
    {
        await fixture.Services.GetRequiredService<IAiCoachProvider>().Analyze(CoachOutputValidatorTests.Context(), default);
        await fixture.Services.GetRequiredService<IAiCardGenerationProvider>().GenerateCards(CardRequest, default);
    }
    private static Dictionary<string, string?> Settings() => new() {
        ["AiCoach:Provider"] = "OpenRouter", ["AiCoach:OpenRouter:Model"] = "z-ai/glm-5.3-flash",
        ["AiCoach:OpenRouter:ApiKey"] = "PRIVATE_COACH_KEY", ["AiCoach:OpenRouter:BaseUrl"] = "https://coach.example.test/chat",
        ["AiCoach:OpenRouter:TimeoutSeconds"] = "60", ["AiCoach:MaxOutputTokens"] = "3000", ["AiCoach:ReasoningEffort"] = "low",
        ["AiCardGeneration:Provider"] = "OpenRouter", ["AiCardGeneration:OpenRouter:Model"] = "google/gemma-4-26b-a4b-it",
        ["AiCardGeneration:OpenRouter:ApiKey"] = "PRIVATE_CARD_KEY", ["AiCardGeneration:OpenRouter:BaseUrl"] = "https://cards.example.test/chat"
    };
    private static Fixture Create(Dictionary<string, string?> settings)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var coach = new Capture(None); var cards = new Capture("{\"cards\":[]}");
        var services = new ServiceCollection(); services.AddLogging();
        services.Configure<AiCoachOptions>(config.GetSection("AiCoach"));
        services.Configure<AiCardGenerationOptions>(config.GetSection("AiCardGeneration"));
        services.AddTransient<FakeAiCoachProvider>(); services.AddTransient<IAiCoachProvider, ConfiguredAiCoachProvider>();
        services.AddHttpClient<OpenRouterAiCoachProvider>().ConfigurePrimaryHttpMessageHandler(() => coach);
        services.AddTransient<FakeAiCardGenerationProvider>(); services.AddTransient<IAiCardGenerationProvider, ConfiguredAiCardGenerationProvider>();
        services.AddHttpClient<OpenRouterAiCardGenerationProvider>().ConfigurePrimaryHttpMessageHandler(() => cards);
        return new(services.BuildServiceProvider(), coach, cards);
    }
    private sealed record Fixture(ServiceProvider Services, Capture Coach, Capture Cards) : IDisposable
    { public void Dispose() => Services.Dispose(); }
    private sealed class Capture(string output) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Body { get; private set; }
        public string? Authorization { get; private set; }
        public string? Url { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Body = await request.Content!.ReadAsStringAsync(cancellationToken); Url = request.RequestUri!.ToString();
            Authorization = request.Headers.Authorization?.ToString();
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { content = output } } } })) };
        }
    }
}

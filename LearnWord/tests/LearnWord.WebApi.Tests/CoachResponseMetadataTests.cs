using System.Net;
using System.Text.Json;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.BL.Services;
using LearnWord.DAL.Models;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using LearnWord.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace LearnWord.WebApi.Tests;

public class CoachResponseMetadataTests
{
    private const string Valid = """{"decision":"None","summary":"","observations":[],"interventions":[]}""";

    [Theory]
    [InlineData("stop", "stop")]
    [InlineData("length", "length")]
    [InlineData("content_filter", "content_filter")]
    [InlineData("PRIVATE_FINISH_REASON", "Other")]
    public async Task ProviderKeepsOnlyAllowlistedFinishReasonAndNumericUsage(string finish, string expected)
    {
        var body = JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = finish, native_finish_reason = "PRIVATE_NATIVE_REASON", message = new { content = Valid } } },
            usage = new { prompt_tokens = 100, completion_tokens = 3000, total_tokens = 3100, completion_tokens_details = new { reasoning_tokens = 2500, private_text = "PRIVATE_USAGE" } } });
        var response = await Provider(body).AnalyzeWithDiagnostics(CoachOutputValidatorTests.Context(), default);
        Assert.Equal(Valid, response.Content);
        var metadata = Assert.IsType<CoachProviderResponseDiagnosticsDto>(response.ProviderResponse);
        Assert.Equal(200, metadata.HttpStatus);
        Assert.Equal(expected, metadata.FinishReason);
        Assert.Equal(100, metadata.PromptTokens);
        Assert.Equal(3000, metadata.CompletionTokens);
        Assert.Equal(3100, metadata.TotalTokens);
        Assert.Equal(2500, metadata.ReasoningTokens);
        Assert.DoesNotContain("PRIVATE_", JsonSerializer.Serialize(metadata));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"prompt_tokens\":-1,\"completion_tokens\":\"PRIVATE_TOKEN\",\"total_tokens\":1.5,\"completion_tokens_details\":{\"reasoning_tokens\":2147483648}}")]
    [InlineData("null")]
    [InlineData("\"PRIVATE_USAGE\"")]
    public async Task MissingOrInvalidUsageStaysUnknownRatherThanInvented(string usage)
    {
        var body = "{\"choices\":[{\"message\":{\"content\":" + JsonSerializer.Serialize(Valid) + "}}],\"usage\":" + usage + "}";
        var response = await Provider(body).AnalyzeWithDiagnostics(CoachOutputValidatorTests.Context(), default);
        var metadata = Assert.IsType<CoachProviderResponseDiagnosticsDto>(response.ProviderResponse);
        Assert.Null(metadata.FinishReason);
        Assert.Null(metadata.PromptTokens); Assert.Null(metadata.CompletionTokens);
        Assert.Null(metadata.TotalTokens); Assert.Null(metadata.ReasoningTokens);
    }

    [Theory]
    [InlineData("")]
    [InlineData("oversized")]
    public async Task RejectedContentRetainsSafeEnvelopeMetadata(string content)
    {
        if (content == "oversized") content = new string('x', CoachLimits.ResponseCharacters + 1);
        var error = await Assert.ThrowsAsync<UpstreamServiceException>(() => Provider(Envelope(content, "length", 3000)).AnalyzeWithDiagnostics(CoachOutputValidatorTests.Context(), default));
        Assert.Equal("ai_provider_invalid_response", error.ErrorCode);
        var metadata = Assert.IsType<CoachProviderResponseDiagnosticsDto>(CoachFailure.Details(error)!.ProviderResponse);
        Assert.Equal("length", metadata.FinishReason);
        Assert.Equal(3000, metadata.CompletionTokens);
        Assert.Equal(200, metadata.HttpStatus);
    }

    [Fact]
    public async Task MalformedChoiceDoesNotDiscardKnownUsageOrCopyProviderBody()
    {
        var error = await Assert.ThrowsAsync<UpstreamServiceException>(() => Provider("{\"choices\":[],\"usage\":{\"completion_tokens\":3000},\"PRIVATE_FIELD\":\"PRIVATE_BODY\"}")
            .AnalyzeWithDiagnostics(CoachOutputValidatorTests.Context(), default));
        var details = CoachFailure.Details(error)!;
        Assert.Equal("ProviderEnvelope", details.FailureStage);
        Assert.Equal(200, details.ProviderResponse!.HttpStatus);
        Assert.Equal(3000, details.ProviderResponse.CompletionTokens);
        Assert.Null(details.ProviderResponse.FinishReason);
        Assert.DoesNotContain("PRIVATE_", JsonSerializer.Serialize(details.ProviderResponse));
    }

    [Theory]
    [InlineData("```json\n{}\n```", 0, 0)]
    [InlineData("PRIVATE_PREFIX {}", 0, 0)]
    [InlineData("{\n\"summary\":", 1, 10)]
    [InlineData("{\"é\":}", 0, 6)]
    public void MalformedContentRetainsOnlyZeroBasedNumericParserPosition(string content, long line, long position)
    {
        var error = Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(content, CoachOutputValidatorTests.Context()));
        var details = CoachFailure.Details(error)!;
        Assert.Equal("json_malformed", details.ValidationCode);
        Assert.Equal("$", details.ValidationPath);
        Assert.Equal(line, details.JsonLineNumber);
        Assert.Equal(position, details.JsonBytePositionInLine);
        Assert.DoesNotContain("PRIVATE_", error.Message);
    }

    [Fact]
    public async Task LegacyProvidersUseDefaultWrapperAndConfiguredProviderForwardsMetadata()
    {
        IAiCoachProvider legacy = new LegacyProvider();
        var wrapped = await legacy.AnalyzeWithDiagnostics(CoachOutputValidatorTests.Context(), default);
        Assert.Equal(Valid, wrapped.Content);
        Assert.Null(wrapped.ProviderResponse);
        var options = OptionsFactory.Create(new AiCoachOptions { Provider = "OpenRouter", OpenRouter = new() { ApiKey = "PRIVATE_KEY", Model = "same-model", BaseUrl = "https://example.test/chat" } });
        var configured = new ConfiguredAiCoachProvider(new FakeAiCoachProvider(), new OpenRouterAiCoachProvider(new HttpClient(new Handler(Envelope(Valid, "length", 123))), options), options);
        var response = await configured.AnalyzeWithDiagnostics(CoachOutputValidatorTests.Context(), default);
        Assert.Equal("length", response.ProviderResponse!.FinishReason);
        Assert.Equal(123, response.ProviderResponse.CompletionTokens);
        Assert.Equal(Valid, await configured.Analyze(CoachOutputValidatorTests.Context(), default));
    }

    [Fact]
    public async Task SharedProviderHasNoLastResponseMetadataThatCanLeakAcrossCalls()
    {
        var handler = new AlternatingHandler();
        var options = OptionsFactory.Create(new AiCoachOptions { OpenRouter = new() { ApiKey = "PRIVATE_KEY", Model = "same-model", BaseUrl = "https://example.test/chat" } });
        var provider = new OpenRouterAiCoachProvider(new HttpClient(handler), options);
        var calls = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => provider.AnalyzeWithDiagnostics(CoachOutputValidatorTests.Context(), default)));
        foreach (var response in calls)
        {
            var index = int.Parse(response.Content);
            Assert.Equal(index % 2 == 0 ? "length" : "stop", response.ProviderResponse!.FinishReason);
            Assert.Equal(index, response.ProviderResponse.CompletionTokens);
        }
    }

    [Theory]
    [InlineData("```json\n{}\n```", true, true, false, false)]
    [InlineData("PRIVATE_PREFIX {}", false, false, false, true)]
    [InlineData("{\n\"summary\":", false, false, true, false)]
    public async Task SyntaxFailurePersistsSafeResponseAndFormatWithoutContent(string content, bool startsFence, bool endsFence, bool startsObject, bool endsObject)
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed(value: "PRIVATE_CARD_VALUE");
        var before = await LearningSnapshot(db);
        var provider = new MetadataProvider { Response = content, Metadata = new() { HttpStatus = 200, FinishReason = "length", CompletionTokens = 3000, ReasoningTokens = 2800 } };
        var service = Service(db, provider);
        Assert.Equal("Failed", (await service.Analyze(session.Id, new())).Status);
        db.Context.ChangeTracker.Clear();
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.Equal("json_malformed", diagnostic.Analysis.ValidationCode);
        Assert.NotNull(diagnostic.Analysis.JsonLineNumber);
        Assert.NotNull(diagnostic.Analysis.JsonBytePositionInLine);
        Assert.Equal("length", diagnostic.ProviderResponse!.FinishReason);
        Assert.Equal(3000, diagnostic.ProviderResponse.CompletionTokens);
        Assert.Equal(2800, diagnostic.ProviderResponse.ReasoningTokens);
        var format = Assert.IsType<CoachOutputFormatDiagnosticsDto>(diagnostic.OutputFormat);
        Assert.Equal(startsFence, format.StartsWithCodeFence); Assert.Equal(endsFence, format.EndsWithCodeFence);
        Assert.Equal(startsObject, format.StartsWithJsonObject); Assert.Equal(endsObject, format.EndsWithJsonObject);
        Assert.False(format.StartsWithJsonArray); Assert.False(format.EndsWithJsonArray);
        var captured = JsonSerializer.Serialize(diagnostic, CoachOutputValidator.Json);
        Assert.DoesNotContain("PRIVATE_", captured); Assert.DoesNotContain("summary", captured);
        Assert.False(diagnostic.Truncated); // Snapshot trimming is independent of finish_reason=length.
        var saves = db.SaveCalls;
        Assert.Equal(captured, JsonSerializer.Serialize(await service.Diagnostics(session.Id), CoachOutputValidator.Json));
        Assert.Equal(saves, db.SaveCalls);
        Assert.Equal(before, await LearningSnapshot(db));
        Assert.Empty(await db.Context.Set<CoachIntervention>().ToListAsync());
        var row = await db.Context.Set<CoachAnalysis>().SingleAsync(); row.UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        await db.Context.SaveChangesAsync();
        provider.Response = Valid; provider.Metadata = null;
        Assert.Equal("Completed", (await service.Analyze(session.Id, new())).Status);
        var retry = await service.Diagnostics(session.Id);
        Assert.Null(retry.ProviderResponse);
        Assert.Null(retry.Analysis.JsonLineNumber); Assert.Null(retry.Analysis.JsonBytePositionInLine);
        Assert.True(retry.OutputFormat!.StartsWithJsonObject); Assert.True(retry.OutputFormat.EndsWithJsonObject);
        Assert.False(retry.OutputFormat.StartsWithCodeFence);
        Assert.Equal(before, await LearningSnapshot(db));
    }

    [Fact]
    public async Task SuccessfulResponseRetainsEnvelopeMetadataAndWhitespaceAwareFormat()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new MetadataProvider { Response = " \n" + Valid + "\t ", Metadata = new() { HttpStatus = 200, FinishReason = "stop", PromptTokens = 123, CompletionTokens = 45, TotalTokens = 168 } };
        var service = Service(db, provider);
        Assert.Equal("Completed", (await service.Analyze(session.Id, new())).Status);
        db.Context.ChangeTracker.Clear();
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.Equal("stop", diagnostic.ProviderResponse!.FinishReason);
        Assert.Equal(168, diagnostic.ProviderResponse.TotalTokens);
        Assert.Null(diagnostic.Analysis.JsonLineNumber); Assert.Null(diagnostic.Analysis.JsonBytePositionInLine);
        Assert.True(diagnostic.OutputFormat!.StartsWithJsonObject); Assert.True(diagnostic.OutputFormat.EndsWithJsonObject);
        Assert.False(diagnostic.OutputFormat.StartsWithCodeFence); Assert.False(diagnostic.OutputFormat.EndsWithCodeFence);
    }

    [Fact]
    public async Task ProviderContentFailureCarriesItsMetadataIntoStoredDiagnostic()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new MetadataProvider { Failure = CoachFailure.WithDetails(new UpstreamServiceException("PRIVATE_BODY", "ai_provider_invalid_response"),
            new CoachFailureDetails("ProviderContentEmpty", ProviderHttpStatus: 200) { ProviderResponse = new() { HttpStatus = 200, FinishReason = "length", CompletionTokens = 3000 } }) };
        var service = Service(db, provider);
        Assert.Equal("Failed", (await service.Analyze(session.Id, new())).Status);
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.Equal("ProviderContentEmpty", diagnostic.Analysis.FailureStage);
        Assert.Equal("length", diagnostic.ProviderResponse!.FinishReason);
        Assert.Null(diagnostic.OutputFormat); Assert.Null(diagnostic.Analysis.JsonLineNumber);
        Assert.DoesNotContain("PRIVATE_", JsonSerializer.Serialize(diagnostic, CoachOutputValidator.Json));
    }

    [Fact]
    public async Task JsonArrayShapeFailureHasFormatFlagsButNoInventedSyntaxPosition()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var service = Service(db, new MetadataProvider { Response = " \n[] ", Metadata = new() { FinishReason = "stop" } });
        Assert.Equal("Failed", (await service.Analyze(session.Id, new())).Status);
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.Equal("schema_type_invalid", diagnostic.Analysis.ValidationCode);
        Assert.Null(diagnostic.Analysis.JsonLineNumber); Assert.Null(diagnostic.Analysis.JsonBytePositionInLine);
        Assert.True(diagnostic.OutputFormat!.StartsWithJsonArray); Assert.True(diagnostic.OutputFormat.EndsWithJsonArray);
        Assert.False(diagnostic.OutputFormat.StartsWithJsonObject); Assert.False(diagnostic.OutputFormat.EndsWithJsonObject);
    }

    [Fact]
    public void OldSnapshotLeavesNewRuntimeFieldsUnknown()
    {
        var old = JsonSerializer.Deserialize<CoachDiagnosticsDto>("{\"analysis\":{\"status\":\"Failed\",\"validationCode\":\"json_malformed\"}}", CoachOutputValidator.Json)!;
        Assert.Null(old.ProviderResponse); Assert.Null(old.OutputFormat);
        Assert.Null(old.Analysis.JsonLineNumber); Assert.Null(old.Analysis.JsonBytePositionInLine);
    }

    private static CoachService Service(CoachTestDatabase db, IAiCoachProvider provider) => new(db.Context,
        new CoachContextBuilder(db.Context, new LearnerStateCalculator()), provider,
        OptionsFactory.Create(new AiCoachOptions { Enabled = true }), NullLogger<CoachService>.Instance);
    private static async Task<string> LearningSnapshot(CoachTestDatabase db) => JsonSerializer.Serialize(new {
        Cards = await db.Context.Cards.AsNoTracking().Select(c => new { c.Id, c.DueDate, c.IntervalDays, c.EaseFactor, c.ReviewCount, c.LearningRevision }).ToListAsync(),
        Attempts = await db.Context.LearningAttempts.AsNoTracking().OrderBy(a => a.Id).ToListAsync(),
        States = await db.Context.LearnerCardStates.AsNoTracking().ToListAsync() }, CoachOutputValidator.Json);
    private sealed class MetadataProvider : IAiCoachProvider
    {
        public string Provider => "Test"; public string Model => "metadata-fixture";
        public string Response { get; set; } = Valid;
        public CoachProviderResponseDiagnosticsDto? Metadata { get; set; }
        public Exception? Failure { get; set; }
        public Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken) => Task.FromResult(Response);
        public Task<CoachProviderResponse> AnalyzeWithDiagnostics(CoachSessionContext context, CancellationToken cancellationToken)
        {
            if (Failure != null) throw Failure;
            return Task.FromResult(new CoachProviderResponse(Response, Metadata));
        }
    }

    private static OpenRouterAiCoachProvider Provider(string body) => new(new HttpClient(new Handler(body)),
        OptionsFactory.Create(new AiCoachOptions { OpenRouter = new() { ApiKey = "PRIVATE_KEY", Model = "same-model", BaseUrl = "https://example.test/chat" } }));
    private static string Envelope(string content, string finish, int tokens) => JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = finish, message = new { content } } }, usage = new { completion_tokens = tokens } });
    private sealed class Handler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
    }
    private sealed class AlternatingHandler : HttpMessageHandler
    {
        private int next;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var id = Interlocked.Increment(ref next); await Task.Yield();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Envelope(id.ToString(), id % 2 == 0 ? "length" : "stop", id)) };
        }
    }
    private sealed class LegacyProvider : IAiCoachProvider
    {
        public string Provider => "Fake"; public string Model => "legacy";
        public Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken) => Task.FromResult(Valid);
    }
}

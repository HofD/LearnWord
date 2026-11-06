using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LearnWord.BL.Models.Errors;
using LearnWord.CoachEval;
using LearnWord.WebApi.Options;
using LearnWord.WebApi.Services;
using Xunit;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace LearnWord.WebApi.Tests;

public class CoachEvaluationReasoningTests
{
    private const string Valid = """{"decision":"None","summary":"","observations":[],"interventions":[]}""";

    [Theory]
    [InlineData("low")]
    [InlineData("high")]
    [InlineData("max")]
    public void ExplicitLiveEffortIsParsedWithoutChangingCallBudget(string effort)
    {
        var result = EvaluationOptions.Parse(["--live", "--model", "z-ai/glm-5.3-flash", "--reasoning-effort", effort], "test-key");
        Assert.Equal(effort, result.ReasoningEffort);
        Assert.Equal(18, result.PlannedCalls);
        Assert.Null(EvaluationOptions.Parse([], null).ReasoningEffort);
    }

    [Theory]
    [InlineData("--reasoning-effort", "low")]
    [InlineData("--live", "--model", "model", "--reasoning-effort")]
    [InlineData("--live", "--model", "model", "--reasoning-effort", "none")]
    [InlineData("--live", "--model", "model", "--reasoning-effort", "PRIVATE_VALUE")]
    [InlineData("--live", "--model", "model", "--reasoning-effort", "low", "--reasoning-effort", "high")]
    public void UnsupportedMissingDuplicateOrFakeEffortIsRejectedBeforeNetwork(params string[] args)
        => Assert.Throws<ArgumentException>(() => EvaluationOptions.Parse(args, "test-key"));

    [Fact]
    public async Task ActualProviderRequestOnlyAddsReasoningEffortAndPreservesEverythingElse()
    {
        var baseline = new Capture();
        using var baselineClient = EvaluationHttp.CreateClient(null, baseline);
        await Provider(baselineClient).AnalyzeWithDiagnostics(Fixtures.All()[0].Context, default);
        var selected = new Capture();
        using var selectedClient = EvaluationHttp.CreateClient("low", selected);
        await Provider(selectedClient).AnalyzeWithDiagnostics(Fixtures.All()[0].Context, default);
        Assert.Equal(1, baseline.Calls); Assert.Equal(1, selected.Calls);
        var before = JsonNode.Parse(baseline.Body!)!.AsObject();
        var after = JsonNode.Parse(selected.Body!)!.AsObject();
        Assert.False(before.ContainsKey("reasoning"));
        Assert.Equal("low", after["reasoning"]!["effort"]!.GetValue<string>());
        Assert.Single(after["reasoning"]!.AsObject());
        Assert.Equal(3000, after["max_tokens"]!.GetValue<int>());
        Assert.Equal("z-ai/glm-5.3-flash", after["model"]!.GetValue<string>());
        Assert.Equal(OpenRouterAiCoachProvider.SystemPrompt, after["messages"]![0]!["content"]!.GetValue<string>());
        after.Remove("reasoning");
        Assert.True(JsonNode.DeepEquals(before, after));
        Assert.Equal(baseline.Authorization, selected.Authorization);
        Assert.Equal("Bearer PRIVATE_KEY", selected.Authorization);
        Assert.Equal("application/json", selected.MediaType);
        Assert.DoesNotContain("PRIVATE_KEY", selected.Body);
    }

    [Fact]
    public async Task InjectedFieldCannotBypassProductionRequestCharacterLimit()
    {
        var capture = new Capture();
        using var client = EvaluationHttp.CreateClient("low", capture);
        var body = new JsonObject { ["payload"] = "" };
        body["payload"] = new string('x', CoachLimits.PromptCharacters - body.ToJsonString().Length);
        var serialized = body.ToJsonString();
        Assert.Equal(CoachLimits.PromptCharacters, serialized.Length);
        var error = await Assert.ThrowsAsync<UpstreamServiceException>(() => client.PostAsync("https://example.test/chat", new StringContent(serialized, Encoding.UTF8, "application/json")));
        Assert.Equal("coach_context_too_large", error.ErrorCode);
        Assert.Equal("RequestSize", CoachFailure.Details(error)!.FailureStage);
        Assert.DoesNotContain("xxxxx", error.Message);
        Assert.Equal(0, capture.Calls);
    }

    [Fact]
    public async Task HttpAdapterPreservesCancellationWithoutCallingInnerSender()
    {
        var capture = new Capture();
        using var client = EvaluationHttp.CreateClient("low", capture);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.PostAsync("https://example.test/chat", new StringContent("{}"), cts.Token));
        Assert.Equal(0, capture.Calls);
    }

    [Fact]
    public async Task ReportAttributesSelectedEffortWithoutChangingBudgetOrVerdicts()
    {
        var capture = new Capture();
        using var client = EvaluationHttp.CreateClient("low", capture);
        var report = await Evaluation.Run(["z-ai/glm-5.3-flash"], 1, true, _ => Provider(client), reasoningEffort: "low");
        Assert.Equal("low", report.ReasoningEffort);
        Assert.Equal(3000, report.MaxOutputTokens);
        Assert.Equal(6, report.CompletedRuns);
        Assert.Equal(4, report.MissedHelp); // Returning None everywhere remains a failing quality test.
        Assert.Equal(1, report.ExitCode);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"reasoningEffort\":\"low\"", json);
        Assert.Contains("\"maxOutputTokens\":3000", json);
        Assert.DoesNotContain("PRIVATE_KEY", json);
        var defaultCapture = new Capture();
        using var defaultClient = EvaluationHttp.CreateClient(null, defaultCapture);
        var plain = await Evaluation.Run(["model"], 1, true, _ => Provider(defaultClient));
        Assert.Null(plain.ReasoningEffort); // Omitted is unknown/provider default, never implicitly off.
        Assert.False(JsonNode.Parse(defaultCapture.Body!)!.AsObject().ContainsKey("reasoning"));
    }

    [Fact]
    public async Task CoreAndHttpEntryPointsRejectUnsupportedEffortBeforeCreatingProvider()
    {
        Assert.Throws<ArgumentException>(() => EvaluationHttp.CreateClient("none"));
        var calls = 0;
        await Assert.ThrowsAsync<ArgumentException>(() => Evaluation.Run(["model"], 1, true, _ => { calls++; throw new InvalidOperationException(); }, reasoningEffort: "PRIVATE_VALUE"));
        await Assert.ThrowsAsync<ArgumentException>(() => Evaluation.Run(["Fake"], 1, false, _ => { calls++; throw new InvalidOperationException(); }, reasoningEffort: "low"));
        Assert.Equal(0, calls);
    }

    private static OpenRouterAiCoachProvider Provider(HttpClient client) => new(client, OptionsFactory.Create(new AiCoachOptions { ReasoningEffort = null, MaxOutputTokens = 3000,
        Provider = "OpenRouter", OpenRouter = new() { Model = "z-ai/glm-5.3-flash", ApiKey = "PRIVATE_KEY", BaseUrl = "https://example.test/chat", TimeoutSeconds = 60 } }));
    private sealed class Capture : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Body { get; private set; }
        public string? Authorization { get; private set; }
        public string? MediaType { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Authorization = request.Headers.Authorization?.ToString(); MediaType = request.Content.Headers.ContentType?.MediaType;
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { content = Valid } } } })) };
        }
    }
}

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

public class CoachEvaluationOutputBudgetTests
{
    private const string Valid = """{"decision":"None","summary":"","observations":[],"interventions":[]}""";

    [Theory]
    [InlineData("3000", 3000)]
    [InlineData("5000", 5000)]
    public void ExplicitLiveBudgetIsParsedWithoutChangingCallCount(string value, int expected)
    {
        var options = EvaluationOptions.Parse(["--live", "--model", "model", "--max-output-tokens", value], "key");
        Assert.Equal(expected, options.MaxOutputTokens);
        Assert.Equal(18, options.PlannedCalls);
        Assert.Equal(3000, EvaluationOptions.Parse([], null).MaxOutputTokens);
    }

    [Theory]
    [InlineData("--max-output-tokens", "5000")]
    [InlineData("--max-output-tokens", "3000")]
    [InlineData("--live", "--model", "model", "--max-output-tokens")]
    [InlineData("--live", "--model", "model", "--max-output-tokens", "0")]
    [InlineData("--live", "--model", "model", "--max-output-tokens", "6000")]
    [InlineData("--live", "--model", "model", "--max-output-tokens", "PRIVATE_VALUE")]
    [InlineData("--live", "--model", "model", "--max-output-tokens", "3000", "--max-output-tokens", "5000")]
    public void InvalidMissingDuplicateOrFakeBudgetIsRejectedBeforeRequests(params string[] args)
        => Assert.Throws<ArgumentException>(() => EvaluationOptions.Parse(args, "key"));

    [Theory]
    [InlineData(null)]
    [InlineData("low")]
    public async Task ActualProviderRequestOnlyChangesSelectedBudgetAndOptionalEffort(string? effort)
    {
        var baseline = new Capture();
        using var plain = EvaluationHttp.CreateClient(null, baseline);
        await Provider(plain).AnalyzeWithDiagnostics(Fixtures.All()[0].Context, default);
        var selected = new Capture();
        using var client = EvaluationHttp.CreateClient(effort, selected, maxOutputTokens: 5000);
        await Provider(client).AnalyzeWithDiagnostics(Fixtures.All()[0].Context, default);
        var before = JsonNode.Parse(baseline.Body!)!.AsObject();
        var after = JsonNode.Parse(selected.Body!)!.AsObject();
        Assert.Equal(3000, before["max_tokens"]!.GetValue<int>());
        Assert.Equal(5000, after["max_tokens"]!.GetValue<int>());
        if (effort == null) Assert.False(after.ContainsKey("reasoning"));
        else
        {
            Assert.Equal("low", after["reasoning"]!["effort"]!.GetValue<string>());
            Assert.Single(after["reasoning"]!.AsObject());
            after.Remove("reasoning");
        }
        after["max_tokens"] = 3000;
        Assert.True(JsonNode.DeepEquals(before, after));
        Assert.Equal(1, baseline.Calls); Assert.Equal(1, selected.Calls);
        Assert.Equal(baseline.Authorization, selected.Authorization);
        Assert.DoesNotContain("PRIVATE_KEY", selected.Body);
    }

    [Fact]
    public async Task ReportUses5000AndKeepsNoneBenchmarkFailuresVisible()
    {
        var capture = new Capture();
        using var client = EvaluationHttp.CreateClient("low", capture, maxOutputTokens: 5000);
        var report = await Evaluation.Run(["model"], 1, true, _ => Provider(client), reasoningEffort: "low", maxOutputTokens: 5000);
        Assert.Equal(5000, report.MaxOutputTokens); Assert.Equal("low", report.ReasoningEffort);
        Assert.Equal(6, report.CompletedRuns); Assert.Equal(4, report.MissedHelp); Assert.Equal(1, report.ExitCode);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("\"maxOutputTokens\":5000", json);
        Assert.DoesNotContain("PRIVATE_KEY", json);
        Assert.Equal(3000, new EvaluationReport().MaxOutputTokens);
    }

    [Fact]
    public async Task UnsupportedBudgetIsRejectedAtCoreAndHttpBoundariesBeforeProviderCreation()
    {
        Assert.Throws<ArgumentException>(() => EvaluationHttp.CreateClient(null, maxOutputTokens: 6000));
        var calls = 0;
        await Assert.ThrowsAsync<ArgumentException>(() => Evaluation.Run(["model"], 1, true, _ => { calls++; throw new InvalidOperationException(); }, maxOutputTokens: 6000));
        await Assert.ThrowsAsync<ArgumentException>(() => Evaluation.Run(["Fake"], 1, false, _ => { calls++; throw new InvalidOperationException(); }, maxOutputTokens: 5000));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CombinedOverridesStillHonorRequestSizeAndCancellation()
    {
        var capture = new Capture();
        using var client = EvaluationHttp.CreateClient("low", capture, maxOutputTokens: 5000);
        var node = new JsonObject { ["max_tokens"] = 3000, ["payload"] = "" };
        node["payload"] = new string('x', CoachLimits.PromptCharacters - node.ToJsonString().Length);
        var error = await Assert.ThrowsAsync<UpstreamServiceException>(() => client.PostAsync("https://example.test/chat", new StringContent(node.ToJsonString(), Encoding.UTF8, "application/json")));
        Assert.Equal("coach_context_too_large", error.ErrorCode); Assert.Equal(0, capture.Calls);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.PostAsync("https://example.test/chat", new StringContent("{}"), cancelled.Token));
        Assert.Equal(0, capture.Calls);
    }

    private static OpenRouterAiCoachProvider Provider(HttpClient client) => new(client, OptionsFactory.Create(new AiCardGenerationOptions {
        Provider = "OpenRouter", OpenRouter = new() { Model = "deepseek/deepseek-v4.1-flash-1017", ApiKey = "PRIVATE_KEY", BaseUrl = "https://example.test/chat", TimeoutSeconds = 60 } }));
    private sealed class Capture : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Body { get; private set; }
        public string? Authorization { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Authorization = request.Headers.Authorization?.ToString();
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { content = Valid } } } })) };
        }
    }
}

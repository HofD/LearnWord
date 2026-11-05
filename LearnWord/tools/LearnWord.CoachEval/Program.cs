using System.Text.Json;
using LearnWord.CoachEval;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using LearnWord.WebApi.Services;
using Microsoft.Extensions.Options;

if (args is ["--help"])
{
    Console.WriteLine("Coach evaluation: [--live --model provider/model [--model provider/model]] [--repeat 1..5] [--output report.json]");
    Console.WriteLine("Default: Fake self-check only. Live requires LEARNWORD_COACH_EVAL_API_KEY; no .env is read. Maximum 60 sequential calls, no retries.");
    return 0;
}
EvaluationOptions options;
var key = Environment.GetEnvironmentVariable("LEARNWORD_COACH_EVAL_API_KEY");
try { options = EvaluationOptions.Parse(args, key); }
catch (ArgumentException)
{
    Console.Error.WriteLine("Invalid evaluation configuration. Use --help. Live requires --live, at least one --model, and LEARNWORD_COACH_EVAL_API_KEY. No requests sent.");
    return 2;
}
try
{
    var probe = Path.Combine(Environment.CurrentDirectory, ".coach-eval-" + Guid.NewGuid().ToString("N") + ".tmp");
    using (File.Create(probe)) { }
    File.Delete(probe);
}
catch (Exception)
{
    Console.Error.WriteLine("Report directory is not writable. No requests sent.");
    return 2;
}
Console.WriteLine($"Mode={ (options.Live ? "LiveModelEvaluation" : "FakeSelfCheck (not model-quality evidence)") } PlannedRuns={options.PlannedCalls} Prompt={CoachLimits.PromptVersion} MaxOutputTokens=3000");
using var cancelled = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelled.Cancel(); };
using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
IAiCoachProvider Factory(string model) => options.Live
    ? new OpenRouterAiCoachProvider(http, Microsoft.Extensions.Options.Options.Create(new AiCardGenerationOptions {
        Provider = "OpenRouter", OpenRouter = new OpenRouterOptions { Model = model, ApiKey = key!, BaseUrl = "https://openrouter.ai/api/v1/chat/completions", TimeoutSeconds = 60 } }))
    : new FakeAiCoachProvider();
var report = await Evaluation.Run(options.Models, options.Repeats, options.Live, Factory, cancelled.Token, progress: update => {
    var prefix = $"[{DateTimeOffset.UtcNow:HH:mm:ss} UTC] [{update.RunNumber}/{update.PlannedRuns}] {update.Model} {update.CaseId} #{update.Repeat}";
    if (update.Result is not { } result)
        Console.WriteLine($"{prefix}: starting (provider timeout 60s)");
    else
        Console.WriteLine($"{prefix}: {result.Outcome}; contractValid={result.ContractValid} finish={result.ProviderResponse?.FinishReason ?? "unknown"} durationMs={result.DurationMs} stage={result.FailureStage ?? "none"} rule={result.ValidationCode ?? "none"}");
    Console.Out.Flush();
});
Console.WriteLine($"Completed={report.CompletedRuns}/{report.PlannedRuns} Passed={report.Passed} MissedHelp={report.MissedHelp} UnexpectedHelp={report.UnexpectedHelp} ContractFailures={report.ContractFailures} ProviderFailures={report.ProviderFailures} EvaluationErrors={report.EvaluationErrors} Cancelled={report.Cancelled}");
foreach (var group in report.ModelSummaries)
    Console.WriteLine($"Model={group.Model} Completed={group.CompletedRuns}/{group.PlannedRuns} Passed={group.Passed} MissedHelp={group.MissedHelp} UnexpectedHelp={group.UnexpectedHelp} ContractFailures={group.ContractFailures} ProviderFailures={group.ProviderFailures} EvaluationErrors={group.EvaluationErrors}");
Console.WriteLine("Scope: automated abstention/evidence/contract checks; not a semantic teaching-quality assessment.");
try
{
    var directory = Path.GetDirectoryName(Path.GetFullPath(options.Output));
    if (directory != null) Directory.CreateDirectory(directory);
    await File.WriteAllTextAsync(options.Output, JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }) + "\n");
}
catch (Exception)
{
    Console.Error.WriteLine("Could not write evaluation report.");
    return 2;
}
return report.ExitCode;

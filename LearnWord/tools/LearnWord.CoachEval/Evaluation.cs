using System.Diagnostics;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Services;
namespace LearnWord.CoachEval;

public sealed record EvaluationResult(string CaseId, string Model, int Repeat, string Outcome, bool ContractValid,
    long DurationMs, CoachProviderResponseDiagnosticsDto? ProviderResponse = null,
    string? FailureStage = null, string? ValidationCode = null, string? ValidationPath = null);
public sealed record EvaluationProgress(int RunNumber, int PlannedRuns, string Model, string CaseId, int Repeat, EvaluationResult? Result = null);
public sealed record ModelEvaluationSummary(string Model, int PlannedRuns, int CompletedRuns, int Passed, int MissedHelp, int UnexpectedHelp, int ContractFailures, int ProviderFailures, int EvaluationErrors);
public sealed class EvaluationReport
{
    public string Mode { get; init; } = "FakeSelfCheck";
    public string PromptVersion { get; init; } = CoachLimits.PromptVersion;
    public int PlannedRuns { get; init; }
    public bool Cancelled { get; set; }
    public IReadOnlyList<string> Models { get; init; } = [];
    public int RunsPerModel { get; init; }
    public IReadOnlyList<ModelEvaluationSummary> ModelSummaries => Models.Select(model => {
        var rows = Results.Where(r => r.Model == model).ToList();
        return new ModelEvaluationSummary(model, RunsPerModel, rows.Count, rows.Count(r => r.Outcome == "pass"),
            rows.Count(r => r.Outcome == "missed_help"), rows.Count(r => r.Outcome == "unexpected_help"),
            rows.Count(r => r.Outcome == "contract_failure"), rows.Count(r => r.Outcome == "provider_failure"), rows.Count(r => r.Outcome == "evaluation_error"));
    }).ToList();
    public List<EvaluationResult> Results { get; } = [];
    public int CompletedRuns => Results.Count;
    public int Passed => Results.Count(r => r.Outcome == "pass");
    public int MissedHelp => Results.Count(r => r.Outcome == "missed_help");
    public int UnexpectedHelp => Results.Count(r => r.Outcome == "unexpected_help");
    public int ContractFailures => Results.Count(r => r.Outcome == "contract_failure");
    public int ProviderFailures => Results.Count(r => r.Outcome == "provider_failure");
    public int EvaluationErrors => Results.Count(r => r.Outcome == "evaluation_error");
    public int ExitCode => Cancelled ? 130 : CompletedRuns == PlannedRuns && Passed == PlannedRuns ? 0 : 1;
}
public static class Evaluation
{
    public static EvaluationResult Score(EvaluationCase test, string model, int repeat, string content, long durationMs = 0,
        CoachProviderResponseDiagnosticsDto? metadata = null)
    {
        try
        {
            var result = CoachOutputValidator.ParseAndValidate(content, test.Context);
            var help = result.Decision == "Intervention";
            return new(test.Id, model, repeat, help == test.ExpectHelp ? "pass" : test.ExpectHelp ? "missed_help" : "unexpected_help", true, durationMs, metadata);
        }
        catch (UpstreamServiceException ex)
        {
            var d = CoachFailure.Details(ex);
            return new(test.Id, model, repeat, "contract_failure", false, durationMs, metadata, d?.FailureStage, d?.ValidationCode, d?.ValidationPath);
        }
    }
    public static async Task<EvaluationReport> Run(IReadOnlyList<string> models, int repeats, bool live,
        Func<string, IAiCoachProvider> providerFactory, CancellationToken cancellationToken = default,
        Action<EvaluationProgress>? progress = null)
    {
        var cases = Fixtures.All();
        if (models.Count is < 1 or > 3 || repeats is < 1 or > 5 || models.Count * repeats * cases.Count > EvaluationOptions.MaximumCalls) throw new ArgumentException();
        var report = new EvaluationReport { Mode = live ? "LiveModelEvaluation" : "FakeSelfCheck", PlannedRuns = models.Count * repeats * cases.Count, Models = models, RunsPerModel = repeats * cases.Count };
        foreach (var model in models)
        {
            var provider = providerFactory(model);
            foreach (var test in cases)
            for (var repeat = 1; repeat <= repeats; repeat++)
            {
                if (cancellationToken.IsCancellationRequested) { report.Cancelled = true; return report; }
                var runNumber = report.CompletedRuns + 1;
                progress?.Invoke(new(runNumber, report.PlannedRuns, model, test.Id, repeat));
                var watch = Stopwatch.StartNew();
                try
                {
                    var response = await provider.AnalyzeWithDiagnostics(test.Context, cancellationToken).WaitAsync(cancellationToken);
                    report.Results.Add(Score(test, model, repeat, response.Content, watch.ElapsedMilliseconds, response.ProviderResponse));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                { report.Cancelled = true; return report; }
                catch (UpstreamServiceException ex)
                {
                    var d = CoachFailure.Details(ex);
                    report.Results.Add(new(test.Id, model, repeat, "provider_failure", false, watch.ElapsedMilliseconds, d?.ProviderResponse,
                        d?.FailureStage, d?.ValidationCode, d?.ValidationPath));
                }
                catch (Exception)
                { report.Results.Add(new(test.Id, model, repeat, "evaluation_error", false, watch.ElapsedMilliseconds)); }
                progress?.Invoke(new(runNumber, report.PlannedRuns, model, test.Id, repeat, report.Results[^1]));
            }
        }
        return report;
    }
}

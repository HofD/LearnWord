using System.Text.RegularExpressions;
namespace LearnWord.CoachEval;

public sealed record EvaluationOptions(bool Live, IReadOnlyList<string> Models, int Repeats, string Output, string? ReasoningEffort = null, int MaxOutputTokens = Evaluation.MaxOutputTokens)
{
    public const int MaximumCalls = 60;
    public int PlannedCalls => Models.Count * Repeats * Fixtures.All().Count;
    public static void ValidateReasoningEffort(string? effort)
    {
        if (effort is not (null or "low" or "high" or "max")) throw new ArgumentException("Unsupported evaluation reasoning effort.");
    }
    public static void ValidateMaxOutputTokens(int value)
    {
        if (value is not (3000 or 5000)) throw new ArgumentException("Evaluation output budget must be 3000 or 5000.");
    }
    public static EvaluationOptions Parse(string[] args, string? liveKey)
    {
        var live = false; var repeats = 3; var output = "coach-eval-report.json"; var models = new List<string>(); string? reasoningEffort = null; var maxOutputTokens = Evaluation.MaxOutputTokens;
        var seen = new HashSet<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is not ("--live" or "--model" or "--repeat" or "--output" or "--reasoning-effort" or "--max-output-tokens")) throw new ArgumentException();
            if (arg != "--model" && !seen.Add(arg)) throw new ArgumentException();
            if (arg == "--live") { live = true; continue; }
            if (++i >= args.Length) throw new ArgumentException();
            var value = args[i];
            switch (arg)
            {
                case "--model":
                    if (models.Count >= 3 || value.Length > 100 || !Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9._:/-]*$", RegexOptions.CultureInvariant) || models.Contains(value)) throw new ArgumentException();
                    models.Add(value); break;
                case "--reasoning-effort": ValidateReasoningEffort(value); reasoningEffort = value; break;
                case "--max-output-tokens":
                    if (!int.TryParse(value, out maxOutputTokens)) throw new ArgumentException();
                    ValidateMaxOutputTokens(maxOutputTokens); break;
                case "--repeat": if (!int.TryParse(value, out repeats) || repeats is < 1 or > 5) throw new ArgumentException(); break;
                case "--output": if (value.Length > 120 || !Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9._-]*\\.json$", RegexOptions.CultureInvariant)) throw new ArgumentException(); output = value; break;
            }
        }
        if (live && (models.Count == 0 || string.IsNullOrWhiteSpace(liveKey))) throw new ArgumentException();
        if (!live && (models.Count > 0 || reasoningEffort != null || seen.Contains("--max-output-tokens"))) throw new ArgumentException();
        if (!live) models.Add("Fake");
        var options = new EvaluationOptions(live, models, repeats, output, reasoningEffort, maxOutputTokens);
        if (options.PlannedCalls > MaximumCalls) throw new ArgumentException();
        return options;
    }
}

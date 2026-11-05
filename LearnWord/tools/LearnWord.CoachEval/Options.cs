using System.Text.RegularExpressions;
namespace LearnWord.CoachEval;

public sealed record EvaluationOptions(bool Live, IReadOnlyList<string> Models, int Repeats, string Output)
{
    public const int MaximumCalls = 60;
    public int PlannedCalls => Models.Count * Repeats * Fixtures.All().Count;
    public static EvaluationOptions Parse(string[] args, string? liveKey)
    {
        var live = false; var repeats = 3; var output = "coach-eval-report.json"; var models = new List<string>();
        var seen = new HashSet<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is not ("--live" or "--model" or "--repeat" or "--output")) throw new ArgumentException();
            if (arg != "--model" && !seen.Add(arg)) throw new ArgumentException();
            if (arg == "--live") { live = true; continue; }
            if (++i >= args.Length) throw new ArgumentException();
            var value = args[i];
            switch (arg)
            {
                case "--model":
                    if (models.Count >= 3 || value.Length > 100 || !Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9._:/-]*$", RegexOptions.CultureInvariant) || models.Contains(value)) throw new ArgumentException();
                    models.Add(value); break;
                case "--repeat": if (!int.TryParse(value, out repeats) || repeats is < 1 or > 5) throw new ArgumentException(); break;
                case "--output": if (value.Length > 120 || !Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9._-]*\\.json$", RegexOptions.CultureInvariant)) throw new ArgumentException(); output = value; break;
            }
        }
        if (live && (models.Count == 0 || string.IsNullOrWhiteSpace(liveKey))) throw new ArgumentException();
        if (!live && models.Count > 0) throw new ArgumentException();
        if (!live) models.Add("Fake");
        var options = new EvaluationOptions(live, models, repeats, output);
        if (options.PlannedCalls > MaximumCalls) throw new ArgumentException();
        return options;
    }
}

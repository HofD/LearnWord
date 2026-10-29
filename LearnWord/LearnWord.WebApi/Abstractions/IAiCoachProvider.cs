using LearnWord.WebApi.Services;
namespace LearnWord.WebApi.Abstractions;
public interface IAiCoachProvider
{
    string Provider { get; }
    string Model { get; }
    Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken);
}

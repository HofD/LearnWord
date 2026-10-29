using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;
namespace LearnWord.Identity.Services;
public class CoachHttpService : UpstreamHttpService<CoachHttpService>, ICoachHttpService
{
    private readonly string url;
    public CoachHttpService(IConfiguration configuration, ILogger<CoachHttpService>? logger = null) : base(logger)
    {
        var collections = configuration["LwServicesRoutes:CollectionsRoute"]?.TrimEnd('/') ?? throw new InvalidOperationException("CollectionsRoute configuration is missing.");
        url = new Uri(new Uri(collections + "/"), "../learning").AbsoluteUri;
    }
    public Task<CoachAnalysisDto> Analyze(Guid id, AnalyzeCoachRequest request) => Post<CoachAnalysisDto>($"sessions/{id}/coach-analysis", request);
    public Task<CoachAnalysisDto> Analysis(Guid id) => Get<CoachAnalysisDto>($"sessions/{id}/coach-analysis");
    public Task<int> Scope(Guid id) => Get<int>($"interventions/{id}/scope");
    public Task<CoachInterventionDto> Intervention(Guid id) => Get<CoachInterventionDto>($"interventions/{id}");
    public Task<CoachInterventionDto> Transition(Guid id, string action) => Post<CoachInterventionDto>($"interventions/{id}/{action}", new { });
    public Task<CoachAnswerDto> Answer(Guid id, Guid exerciseId, CoachAnswerRequest request) => Post<CoachAnswerDto>($"interventions/{id}/exercises/{exerciseId}/answer", request);
    private Task<T> Get<T>(string path) => SendForJson<T>("Coach", "ReadCoach", $"{url}/{path}", () => HttpClient.GetAsync($"{url}/{path}"), "coach_empty_response", "Coach read failed.");
    private Task<T> Post<T>(string path, object payload) => SendForJson<T>("Coach", "WriteCoach", $"{url}/{path}", () => HttpClient.PostAsJsonAsync($"{url}/{path}", payload), "coach_empty_response", "Coach operation failed.");
}

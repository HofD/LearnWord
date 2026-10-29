using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;
namespace LearnWord.Identity.Services;
public class LearningHttpService : UpstreamHttpService<LearningHttpService>, ILearningHttpService
{
    private readonly string url;
    public LearningHttpService(IConfiguration configuration, ILogger<LearningHttpService>? logger = null) : base(logger)
    {
        var collections = configuration["LwServicesRoutes:CollectionsRoute"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("CollectionsRoute configuration is missing.");
        url = new Uri(new Uri(collections + "/"), "../learning").AbsoluteUri;
    }
    public Task<TodayPlanDto> Today(int id) => Get<TodayPlanDto>($"collections/{id}/today");
    public Task<LearnerSummaryDto> Summary(int id) => Get<LearnerSummaryDto>($"collections/{id}/summary");
    public Task<LearningSessionDto> Start(int id) => Post<LearningSessionDto>($"collections/{id}/sessions");
    public Task<LearningSessionDto> Session(Guid id) => Get<LearningSessionDto>($"sessions/{id}");
    public Task<LearningSessionDto> Complete(Guid id) => Post<LearningSessionDto>($"sessions/{id}/complete");
    public Task<LearnerCardStateDto> State(int id) => Get<LearnerCardStateDto>($"cards/{id}/state");
    public Task<PagedResult<LearningAttemptDto>> Attempts(int id, int page, int pageSize)
        => Get<PagedResult<LearningAttemptDto>>($"cards/{id}/attempts?page={page}&pageSize={pageSize}");
    private Task<T> Get<T>(string path) => SendForJson<T>("Learning", "ReadLearning", $"{url}/{path}",
        () => HttpClient.GetAsync($"{url}/{path}"), "learning_empty_response", "Learning read failed.");
    private Task<T> Post<T>(string path) => SendForJson<T>("Learning", "WriteLearning", $"{url}/{path}",
        () => HttpClient.PostAsync($"{url}/{path}", null), "learning_empty_response", "Learning operation failed.");
}

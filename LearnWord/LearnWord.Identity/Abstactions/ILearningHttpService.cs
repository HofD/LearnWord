using LearnWord.BL.Models.Dto;
namespace LearnWord.Identity.Abstactions;
public interface ILearningHttpService
{
    Task<TodayPlanDto> Today(int collectionId);
    Task<LearnerSummaryDto> Summary(int collectionId);
    Task<LearningSessionDto> Start(int collectionId);
    Task<LearningSessionDto> Session(Guid sessionId);
    Task<LearningSessionDto> Complete(Guid sessionId);
    Task<LearnerCardStateDto> State(int cardId);
    Task<PagedResult<LearningAttemptDto>> Attempts(int cardId, int page, int pageSize);
}

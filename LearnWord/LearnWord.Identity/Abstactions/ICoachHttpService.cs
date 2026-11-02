using LearnWord.BL.Models.Dto;
namespace LearnWord.Identity.Abstactions;
public interface ICoachHttpService
{
    Task<CoachAnalysisDto> Analyze(Guid sessionId, AnalyzeCoachRequest request);
    Task<CoachAnalysisDto> Analysis(Guid sessionId);
    Task<CoachDiagnosticsDto> Diagnostics(Guid sessionId);
    Task<int> Scope(Guid id);
    Task<CoachInterventionDto> Intervention(Guid id);
    Task<CoachInterventionDto> Transition(Guid id, string action);
    Task<CoachAnswerDto> Answer(Guid id, Guid exerciseId, CoachAnswerRequest request);
}

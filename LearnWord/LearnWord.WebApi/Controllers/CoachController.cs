using LearnWord.BL.Models.Dto;
using LearnWord.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
namespace LearnWord.WebApi.Controllers;
[ApiController]
[Route("learning")]
public class CoachController : ControllerBase
{
    private readonly CoachService service;
    public CoachController(CoachService service) { this.service = service; }
    [HttpPost("sessions/{sessionId:guid}/coach-analysis")]
    public Task<CoachAnalysisDto> Analyze(Guid sessionId, [FromBody] AnalyzeCoachRequest request) => service.Analyze(sessionId, request);
    [HttpGet("sessions/{sessionId:guid}/coach-analysis")]
    public Task<CoachAnalysisDto> Analysis(Guid sessionId) => service.Analysis(sessionId);
    [HttpGet("interventions/{id:guid}")]
    public Task<CoachInterventionDto> Intervention(Guid id) => service.Intervention(id);
    [HttpGet("interventions/{id:guid}/scope")]
    public Task<int> Scope(Guid id) => service.Scope(id);
    [HttpPost("interventions/{id:guid}/accept")]
    public Task<CoachInterventionDto> Accept(Guid id) => service.Accept(id);
    [HttpPost("interventions/{id:guid}/dismiss")]
    public Task<CoachInterventionDto> Dismiss(Guid id) => service.Dismiss(id);
    [HttpPost("interventions/{id:guid}/complete")]
    public Task<CoachInterventionDto> Complete(Guid id) => service.Complete(id);
    [HttpPost("interventions/{id:guid}/exercises/{exerciseId:guid}/answer")]
    public Task<CoachAnswerDto> Answer(Guid id, Guid exerciseId, [FromBody] CoachAnswerRequest request) => service.Answer(id, exerciseId, request);
}

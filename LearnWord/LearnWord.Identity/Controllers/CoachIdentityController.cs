using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Services;
using Microsoft.AspNetCore.Mvc;
namespace LearnWord.Identity.Controllers;
[ApiController]
[Route("learning")]
public class CoachIdentityController : ControllerBase
{
    private readonly CoachIdentityService service;
    public CoachIdentityController(CoachIdentityService service) { this.service = service; }
    private string? UserId => HttpContext.Items["UserId"]?.ToString();
    [HttpPost("sessions/{sessionId:guid}/coach-analysis")]
    public async Task<ActionResult<CoachAnalysisDto>> Analyze(Guid sessionId, [FromBody] AnalyzeCoachRequest request)
        => UserId is string u ? Ok(await service.Analyze(sessionId, request, u)) : Unauthorized();
    [HttpGet("sessions/{sessionId:guid}/coach-diagnostics")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<CoachDiagnosticsDto>> Diagnostics(Guid sessionId)
        => UserId is string u ? Ok(await service.Diagnostics(sessionId, u)) : Unauthorized();
    [HttpGet("sessions/{sessionId:guid}/coach-analysis")]
    public async Task<ActionResult<CoachAnalysisDto>> Analysis(Guid sessionId)
        => UserId is string u ? Ok(await service.Analysis(sessionId, u)) : Unauthorized();
    [HttpGet("interventions/{id:guid}")]
    public async Task<ActionResult<CoachInterventionDto>> Intervention(Guid id)
        => UserId is string u ? Ok(await service.Intervention(id, u)) : Unauthorized();
    [HttpPost("interventions/{id:guid}/accept")]
    public async Task<ActionResult<CoachInterventionDto>> Accept(Guid id)
        => UserId is string u ? Ok(await service.Transition(id, "accept", u)) : Unauthorized();
    [HttpPost("interventions/{id:guid}/dismiss")]
    public async Task<ActionResult<CoachInterventionDto>> Dismiss(Guid id)
        => UserId is string u ? Ok(await service.Transition(id, "dismiss", u)) : Unauthorized();
    [HttpPost("interventions/{id:guid}/complete")]
    public async Task<ActionResult<CoachInterventionDto>> Complete(Guid id)
        => UserId is string u ? Ok(await service.Transition(id, "complete", u)) : Unauthorized();
    [HttpPost("interventions/{id:guid}/exercises/{exerciseId:guid}/answer")]
    public async Task<ActionResult<CoachAnswerDto>> Answer(Guid id, Guid exerciseId, [FromBody] CoachAnswerRequest request)
        => UserId is string u ? Ok(await service.Answer(id, exerciseId, request, u)) : Unauthorized();
}

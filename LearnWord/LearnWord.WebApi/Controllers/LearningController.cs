using LearnWord.BL.Models.Dto;
using LearnWord.BL.Services;
using Microsoft.AspNetCore.Mvc;
namespace LearnWord.WebApi.Controllers;

[ApiController]
[Route("learning")]
public class LearningController : ControllerBase
{
    private readonly LearningService service;
    public LearningController(LearningService service) { this.service = service; }
    [HttpGet("collections/{collectionId:int}/today")]
    public Task<TodayPlanDto> Today(int collectionId) => service.Today(collectionId);
    [HttpGet("collections/{collectionId:int}/summary")]
    public Task<LearnerSummaryDto> Summary(int collectionId) => service.Summary(collectionId);
    [HttpPost("collections/{collectionId:int}/sessions")]
    public Task<LearningSessionDto> Start(int collectionId) => service.Start(collectionId);
    [HttpGet("sessions/{sessionId:guid}")]
    public Task<LearningSessionDto> Session(Guid sessionId) => service.Session(sessionId);
    [HttpPost("sessions/{sessionId:guid}/complete")]
    public Task<LearningSessionDto> Complete(Guid sessionId) => service.Complete(sessionId);
    [HttpGet("cards/{cardId:int}/state")]
    public Task<LearnerCardStateDto> State(int cardId) => service.State(cardId);
    [HttpGet("cards/{cardId:int}/attempts")]
    public Task<PagedResult<LearningAttemptDto>> Attempts(int cardId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => service.Attempts(cardId, page, pageSize);
}

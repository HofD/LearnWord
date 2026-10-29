using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Services;
using Microsoft.AspNetCore.Mvc;
namespace LearnWord.Identity.Controllers;

[ApiController]
[Route("learning")]
public class LearningIdentityController : ControllerBase
{
    private readonly LearningIdentityService service;
    public LearningIdentityController(LearningIdentityService service) { this.service = service; }
    private string? UserId => HttpContext.Items["UserId"]?.ToString();
    [HttpGet("collections/{collectionId:int}/today")]
    public async Task<ActionResult<TodayPlanDto>> Today(int collectionId)
        => UserId is string userId ? Ok(await service.Today(collectionId, userId)) : Unauthorized();
    [HttpGet("collections/{collectionId:int}/summary")]
    public async Task<ActionResult<LearnerSummaryDto>> Summary(int collectionId)
        => UserId is string userId ? Ok(await service.Summary(collectionId, userId)) : Unauthorized();
    [HttpPost("collections/{collectionId:int}/sessions")]
    public async Task<ActionResult<LearningSessionDto>> Start(int collectionId)
        => UserId is string userId ? Ok(await service.Start(collectionId, userId)) : Unauthorized();
    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<ActionResult<LearningSessionDto>> Session(Guid sessionId)
        => UserId is string userId ? Ok(await service.Session(sessionId, userId)) : Unauthorized();
    [HttpPost("sessions/{sessionId:guid}/complete")]
    public async Task<ActionResult<LearningSessionDto>> Complete(Guid sessionId)
        => UserId is string userId ? Ok(await service.Complete(sessionId, userId)) : Unauthorized();
    [HttpGet("cards/{cardId:int}/state")]
    public async Task<ActionResult<LearnerCardStateDto>> State(int cardId)
        => UserId is string userId ? Ok(await service.State(cardId, userId)) : Unauthorized();
    [HttpGet("cards/{cardId:int}/attempts")]
    public async Task<ActionResult<PagedResult<LearningAttemptDto>>> Attempts(int cardId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => UserId is string userId ? Ok(await service.Attempts(cardId, page, pageSize, userId)) : Unauthorized();
}

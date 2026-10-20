using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.Identity.Controllers;

[ApiController]
[Route("social/collections")]
public class SocialCollectionsController : ControllerBase
{
    private readonly SocialCollectionIdentityService service;
    public SocialCollectionsController(SocialCollectionIdentityService service) { this.service = service; }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PublicCollectionSummary>>> Explore([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        if (HttpContext.Items["UserId"]?.ToString() is not string) return Unauthorized();
        return Ok(await service.Explore(page, pageSize, search));
    }

    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<PublicCollectionDetails>> Get(Guid publicId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
    {
        if (HttpContext.Items["UserId"]?.ToString() is not string userId) return Unauthorized();
        return Ok(await service.Get(publicId, page, pageSize, userId, search));
    }

    [HttpPost("{publicId:guid}/fork")]
    public async Task<ActionResult<CollectionDto>> Fork(Guid publicId)
    {
        if (HttpContext.Items["UserId"]?.ToString() is not string userId) return Unauthorized();
        return StatusCode(StatusCodes.Status201Created, await service.Fork(publicId, userId));
    }
}

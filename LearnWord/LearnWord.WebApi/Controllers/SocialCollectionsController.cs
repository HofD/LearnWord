using LearnWord.BL.Models.Dto;
using LearnWord.BL.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.WebApi.Controllers;

// Internal service: reachable publicly only through the authenticated ownership facade.
[ApiController]
[Route("social/collections")]
public class SocialCollectionsController : ControllerBase
{
    private readonly SocialCollectionService service;
    public SocialCollectionsController(SocialCollectionService service) { this.service = service; }

    [HttpGet]
    public Task<PagedResult<SocialCollectionRecord>> Explore([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? search = null) => service.Explore(page, pageSize, search);

    [HttpGet("{publicId:guid}")]
    public Task<SocialCollectionPage> Get(Guid publicId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, [FromQuery] string? search = null) => service.Get(publicId, page, pageSize, search);

    [HttpPost("{publicId:guid}/fork")]
    public Task<CollectionDto> Fork(Guid publicId) => service.Fork(publicId);
}

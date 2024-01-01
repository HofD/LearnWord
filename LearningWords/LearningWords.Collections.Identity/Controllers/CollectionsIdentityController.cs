using LearningWords.BL.Models.Dto;
using LearningWords.Collections.Identity.Abstactions;
using Microsoft.AspNetCore.Mvc;

namespace LearningWords.Collections.Identity.Controllers
{
    [ApiController]
    [Route("collections")]
    public class CollectionsIdentityController : ControllerBase
    {
        private readonly ICollectionIdentityService collectionIdentityService;

        public CollectionsIdentityController(ICollectionIdentityService collectionIdentityService) 
        {
            this.collectionIdentityService = collectionIdentityService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CollectionDto>> Get(int id)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await collectionIdentityService.Get(id, userId);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<CollectionListDto>> GetAll()
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            return Ok(await collectionIdentityService.GetAll(userId));
        }

        [HttpPost]
        public async Task<ActionResult<CollectionDto>> Add(CollectionCreateDto collection)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            return CreatedAtAction(nameof(Add), await collectionIdentityService.Add(collection, userId));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            await collectionIdentityService.Remove(id, userId);

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CollectionDto>> Rename(int id, CollectionRenameDto collectionRenameDto)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await collectionIdentityService.Rename(id, collectionRenameDto, userId);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
    }
}

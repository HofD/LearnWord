using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.Identity.Controllers
{
    [ApiController]
    [Route("cards")]
    public class WordsIdentityController : ControllerBase
    {
        private readonly IWordIdentityService wordIdentityService;

        public WordsIdentityController(IWordIdentityService wordIdentityService)
        {
            this.wordIdentityService = wordIdentityService;
        }

        [HttpPost("{cardId}/words")]
        public async Task<ActionResult<CardDto>> Add(int cardId, WordCreateDto word)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            return CreatedAtAction(nameof(Add), await wordIdentityService.Add(cardId, word, userId));
        }

        [HttpDelete("{cardId}/words/{id}")]
        public async Task<IActionResult> Remove(int cardId, int id)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await wordIdentityService.Remove(id, cardId, userId);

            if (result)
            {
                return Ok();
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{cardId}/words/{id}")]
        public async Task<ActionResult<WordDto>> Update(int cardId, int id, WordUpdateDto word)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            return Ok(await wordIdentityService.Update(cardId, id, word, userId));
        }
    }
}

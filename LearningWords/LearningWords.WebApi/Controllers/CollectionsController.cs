using LearningWords.BL.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LearningWords.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CollectionsController : ControllerBase
    {
        private readonly ILogger<CollectionsController> _logger;

        public CollectionsController(ILogger<CollectionsController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "Get")]
        public IEnumerable<CollectionDto> Get()
        {
            return null;
        }
    }
}
using LearningWords.BL.Models.Dto;
using LearningWords.BL.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearningWords.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CollectionsController : ControllerBase
    {
        private readonly ILogger<CollectionsController> logger;
        private readonly CollectionService collectionService;

        public CollectionsController(ILogger<CollectionsController> logger, CollectionService collectionService)
        {
            this.logger = logger;
            this.collectionService = collectionService;
        }

        [HttpGet("{id}")]
        public async Task<CollectionDto> Get(int id)
        {
            return await collectionService.Get(id);
        }

        [HttpPost]
        public async Task Create(CollectionCreateDto collection)
        {
            await collectionService.Create(collection);
        }

        [HttpDelete("{id}")]
        public async Task Delete(int id)
        {
            await collectionService.Delete(id);
        }

        [HttpPut("{id}")]
        public async Task Rename(int id, CollectionRenameDto collectionRenameDto)
        {
            await collectionService.Rename(id, collectionRenameDto);
        }
    }
}
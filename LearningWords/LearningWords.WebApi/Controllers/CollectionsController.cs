using LearningWords.BL.Models.Dto;
using LearningWords.BL.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearningWords.WebApi.Controllers
{
    [ApiController]
    [Route("collections")]
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
        public async Task Add(CollectionCreateDto collection)
        {
            await collectionService.Add(collection);
        }

        [HttpDelete("{id}")]
        public async Task Remove(int id)
        {
            await collectionService.Remove(id);
        }

        [HttpPut("{id}")]
        public async Task Rename(int id, CollectionRenameDto collectionRenameDto)
        {
            await collectionService.Rename(id, collectionRenameDto);
        }
    }
}
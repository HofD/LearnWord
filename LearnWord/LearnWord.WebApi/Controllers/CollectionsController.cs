using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.WebApi.Controllers
{
    [ApiController]
    [Route("collections")]
    public class CollectionsController : ControllerBase
    {
        private readonly ILogger<CollectionsController> logger;
        private readonly ICollectionService collectionService;

        public CollectionsController(ILogger<CollectionsController> logger, ICollectionService collectionService)
        {
            this.logger = logger;
            this.collectionService = collectionService;
        }

        [HttpGet("{id}")]
        public async Task<CollectionDto> Get(int id)
        {
            return await collectionService.Get(id);
        }

        [HttpGet]
        public async Task<CollectionListDto> GetList([FromQuery(Name = "ids")] int[] ids)
        {
            return await collectionService.GetList(ids.ToList());
        }

        [HttpPost]
        public async Task<CollectionDto> Add(CollectionCreateDto collection)
        {
            return await collectionService.Add(collection);
        }

        [HttpDelete("{id}")]
        public async Task Remove(int id)
        {
            await collectionService.Remove(id);
        }

        [HttpPut("{id}")]
        public async Task<CollectionDto> Rename(int id, CollectionRenameDto collectionRenameDto)
        {
            return await collectionService.Rename(id, collectionRenameDto);
        }
    }
}
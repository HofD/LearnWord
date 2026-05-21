using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;
using LearnWord.WebApi.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.WebApi.Controllers
{
    [ApiController]
    [Route("collections")]
    public class CollectionsController : ControllerBase
    {
        private readonly ILogger<CollectionsController> logger;
        private readonly ICollectionService collectionService;
        private readonly IAiCardGenerationService aiCardGenerationService;

        public CollectionsController(
            ILogger<CollectionsController> logger,
            ICollectionService collectionService,
            IAiCardGenerationService aiCardGenerationService)
        {
            this.logger = logger;
            this.collectionService = collectionService;
            this.aiCardGenerationService = aiCardGenerationService;
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

        [HttpGet("{id}/review-cards")]
        public async Task<IEnumerable<CardDto>> GetReviewCards(int id)
        {
            return await collectionService.GetReviewCards(id);
        }

        [HttpPost("{collectionId}/ai/generate-cards")]
        public async Task<AiCardGenerationResponse> GenerateAiCards(
            int collectionId,
            AiCardGenerationRequest request,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Generating AI card suggestions for collection {CollectionId}.", collectionId);
            return await aiCardGenerationService.GenerateCards(request, cancellationToken);
        }
    }
}

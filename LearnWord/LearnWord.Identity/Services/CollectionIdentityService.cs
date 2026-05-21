using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.DAL.Models;
using LearnWord.Identity.DAL.Repositories;

namespace LearnWord.Identity.Services
{
    public class CollectionIdentityService : ICollectionIdentityService
    {
        private readonly ICollectionsHttpService collectionsHttpService;
        private readonly CollectionIdentityRepository repository;

        public CollectionIdentityService(ICollectionsHttpService collectionsHttpService, CollectionIdentityRepository repository) 
        {
            this.collectionsHttpService = collectionsHttpService;
            this.repository = repository;
        }

        public async Task<CollectionDto> Add(CollectionCreateDto createDto, string userId)
        {
            var result = await collectionsHttpService.Add(createDto);

            await repository.Add(new CollectionIdentity() { CollectionId = result.Id, UserId = userId });

            return result;
        }

        public async Task<CollectionDto?> Get(int id, string userId)
        {
            var link = await repository.Get(id, userId);

            if (link == null)
            {
                return null;
            }

            return await collectionsHttpService.Get(id);
        }

        public async Task<CollectionListDto> GetAll(string userId)
        {
            var list = await repository.GetAll(userId);

            return await collectionsHttpService.GetList(list.Select(x => x.CollectionId).ToArray());
        }

        public async Task<bool?> Remove(int id, string userId)
        {
            var link = await repository.Get(id, userId);

            if (link == null)
            {
                return null;
            }

            return await collectionsHttpService.Remove(id);
        }

        public async Task<CollectionDto?> Rename(int id, CollectionRenameDto collectionRenameDto, string userId)
        {
            var collection = await Get(id, userId);

            if (collection == null)
            {
                return null;
            }

            return await collectionsHttpService.Rename(id, collectionRenameDto);
        }

        public async Task<IEnumerable<CardDto>> GetCardsForReview(int collectionId, string userId)
        {
            var collection = await Get(collectionId, userId);

            if (collection == null)
            {
                throw new ForbiddenException($"Collection with id {collectionId} does not belong to current user.", "collection_forbidden");
            }

            return await collectionsHttpService.GetCardsForReview(collectionId);
        }

        public async Task<AiCardGenerationResponse> GenerateAiCards(int collectionId, AiCardGenerationRequest request, string userId)
        {
            var link = await repository.Get(collectionId, userId);

            if (link == null)
            {
                throw new ForbiddenException($"Collection with id {collectionId} does not belong to current user.", "collection_forbidden");
            }

            return await collectionsHttpService.GenerateAiCards(collectionId, request);
        }
    }
}

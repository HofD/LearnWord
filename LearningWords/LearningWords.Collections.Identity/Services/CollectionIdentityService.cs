using LearningWords.BL.Models.Dto;
using LearningWords.Collections.Identity.Abstactions;
using LearningWords.Collections.Identity.DAL.Models;
using LearningWords.Collections.Identity.DAL.Repositories;

namespace LearningWords.Collections.Identity.Services
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

        public async Task Remove(int id, string userId)
        {
            var link = await repository.Get(id, userId);

            if (link == null)
            {
                return;
            }

            if (await collectionsHttpService.Remove(id))
            {
                await repository.Remove(id, userId);
            }

            return;
        }

        public async Task<CollectionDto?> Rename(int id, CollectionRenameDto renameDto, string userId)
        {
            var link = await repository.Get(id, userId);

            if (link == null)
            {
                return null;
            }

            return await collectionsHttpService.Rename(id, renameDto);
        }
    }
}

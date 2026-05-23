using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.DAL.Models;
using LearnWord.Identity.DAL.Repositories;

namespace LearnWord.Identity.Services
{
    public class CardIdentityService : ICardIdentityService
    {
        private readonly ICardHttpService cardHttpService;
        private readonly CardIdentityRepository repository;
        private readonly CollectionIdentityRepository collectionIdentityRepository;

        public CardIdentityService(
            ICardHttpService cardHttpService,
            CardIdentityRepository repository,
            CollectionIdentityRepository collectionIdentityRepository)
        {
            this.cardHttpService = cardHttpService;
            this.repository = repository;
            this.collectionIdentityRepository = collectionIdentityRepository;
        }
        public async Task<CardDto> Add(CardCreateDto cardCreateDto, string userId)
        {
            await CheckCollectionIdentity(cardCreateDto.CollectionId, userId);

            var result = await cardHttpService.Add(cardCreateDto);

            await repository.Add(new CardIdentity() { CardId = result.Id, UserId = userId });

            return result;
        }

        public async Task<CardDto> Review(int id, ReviewCardRequest request, string userId)
        {
            await CheckCardIdentity(id, userId);

            return await cardHttpService.Review(id, request);
        }

        public async Task<bool> Remove(int id, string userId)
        {
            await CheckCardIdentity(id, userId);

            return await cardHttpService.Remove(id);
        }

        private async Task CheckCardIdentity(int id, string userId)
        {
            var link = await repository.Get(id, userId);

            if (link == null)
            {
                throw new ForbiddenException($"Card with id {id} does not belong to current user.", "card_forbidden");
            }
        }

        private async Task CheckCollectionIdentity(int id, string userId)
        {
            var link = await collectionIdentityRepository.Get(id, userId);

            if (link == null)
            {
                throw new ForbiddenException($"Collection with id {id} does not belong to current user.", "collection_forbidden");
            }
        }
    }
}

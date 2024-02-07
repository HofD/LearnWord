using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.DAL.Models;
using LearnWord.Identity.DAL.Repositories;

namespace LearnWord.Identity.Services
{
    public class CardIdentityService : ICardIdentityService
    {
        private readonly ICardHttpService cardHttpService;
        private readonly CardIdentityRepository repository;

        public CardIdentityService(ICardHttpService cardHttpService, CardIdentityRepository repository)
        {
            this.cardHttpService = cardHttpService;
            this.repository = repository;
        }
        public async Task<CardDto> Add(CardCreateDto cardCreateDto, string userId)
        {
            var result = await cardHttpService.Add(cardCreateDto);

            await repository.Add(new CardIdentity() { CardId = result.Id, UserId = userId });

            return result;
        }

        public async Task<CardDto> Forget(int id, string userId)
        {
            await CheckCardIdentity(id, userId);

            return await cardHttpService.Forget(id);
        }

        public async Task<CardDto> Learn(int id, string userId)
        {
            await CheckCardIdentity(id, userId);

            return await cardHttpService.Learn(id);
        }

        public async Task<bool?> Remove(int id, string userId)
        {
            await CheckCardIdentity(id, userId);

            return await cardHttpService.Remove(id);
        }

        private async Task CheckCardIdentity(int id, string userId)
        {
            var link = await repository.Get(id, userId);

            if (link == null)
            {
                throw new Exception($"Card with id: {id} isn't belongs you.");
            }
        }
    }
}

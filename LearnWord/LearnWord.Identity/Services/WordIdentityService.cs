using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.DAL.Repositories;

namespace LearnWord.Identity.Services
{
    public class WordIdentityService : IWordIdentityService
    {
        private readonly IWordHttpService wordHttpService;
        private readonly CardIdentityRepository repository;

        public WordIdentityService(IWordHttpService wordHttpService, CardIdentityRepository repository)
        {
            this.wordHttpService = wordHttpService;
            this.repository = repository;
        }

        public async Task<WordDto?> Add(int cardId, WordCreateDto wordCreateDto, string userId)
        {
            await CheckCardIdentity(cardId, userId);

            return await wordHttpService.Add(cardId, wordCreateDto);
        }

        public async Task<bool> Remove(int id, int cardId, string userId)
        {
            await CheckCardIdentity(cardId, userId);

            return await wordHttpService.Remove(id, cardId);
        }

        public async Task<WordDto> Update(int cardId, int id, WordUpdateDto wordUpdateDto, string userId)
        {
            await CheckCardIdentity(cardId, userId);

            return await wordHttpService.Update(cardId, id, wordUpdateDto);
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

using LearnWord.BL.Abstractions;
using LearnWord.BL.Mapping;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;

namespace LearnWord.BL.Services
{
    public class CardService : ICardService
    {
        private readonly CardRepository cardRepository;
        private readonly ObjectMapper mapper;
        private readonly ISpacedRepetitionScheduler scheduler;
        private readonly LearningService learning;

        public CardService(CardRepository cardRepository, ObjectMapper mapper, ISpacedRepetitionScheduler scheduler, LearningService? learning = null)
        {
            this.cardRepository = cardRepository;
            this.mapper = mapper;
            this.scheduler = scheduler;
            this.learning = learning ?? new LearningService(cardRepository.Learning, mapper, new LearnerStateCalculator());
        }
        public async Task<CardDto> Add(CardCreateDto createDto)
        {
            return mapper.Map<CardDto>(await cardRepository.Add(mapper.Map<Card>(createDto)));
        }
        public async Task Remove(int id)
        {
            await cardRepository.Remove(id);
        }
        public async Task<CardDto> Reset(int id)
        {
            var learning = cardRepository.Learning;
            await using var tx = learning.Context.Database.CurrentTransaction == null
                ? await learning.BeginMutation(id) : null;
            var card = await cardRepository.FindById(id, false);

            if (card == null)
            {
                throw new NotFoundException($"Card {id} not found.", "card_not_found");
            }

            scheduler.Reset(card, DateTimeOffset.UtcNow);
            await cardRepository.SaveChangesAsync();
            if (tx != null) await tx.CommitAsync();
            return mapper.Map<CardDto>(card);
        }

        public Task<CardDto> Review(int id, ReviewCardRequest request)
        {
            return learning.Review(id, request, scheduler);
        }
    }
}

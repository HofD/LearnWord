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

        public CardService(CardRepository cardRepository, ObjectMapper mapper, ISpacedRepetitionScheduler scheduler)
        {
            this.cardRepository = cardRepository;
            this.mapper = mapper;
            this.scheduler = scheduler;
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
            var card = await cardRepository.FindById(id, false);

            if (card == null)
            {
                throw new NotFoundException($"Card {id} not found.", "card_not_found");
            }

            scheduler.Reset(card, DateTimeOffset.UtcNow);
            await cardRepository.SaveChangesAsync();

            return mapper.Map<CardDto>(card);
        }

        public async Task<CardDto> Review(int id, ReviewCardRequest request)
        {
            if (request == null || !Enum.TryParse<ReviewOutcome>(request.Outcome, ignoreCase: false, out var outcome))
            {
                throw new BadRequestException(
                    "Outcome must be one of Again, Hard, Good, or Easy.",
                    "invalid_review_outcome");
            }

            var card = await cardRepository.FindById(id);

            if (card == null)
            {
                throw new NotFoundException($"Card {id} not found.", "card_not_found");
            }

            scheduler.ApplyReview(card, outcome, DateTimeOffset.UtcNow);
            await cardRepository.SaveChangesAsync();

            return mapper.Map<CardDto>(card);
        }
    }
}

using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;

namespace LearnWord.BL.Mapping
{
    public class ObjectMapper
    {
        public TDestination Map<TDestination>(object source)
        {
            return source switch
            {
                Collection collection when typeof(TDestination) == typeof(CollectionDto) =>
                    (TDestination)(object)MapCollection(collection),
                CollectionCreateDto dto when typeof(TDestination) == typeof(Collection) =>
                    (TDestination)(object)MapCollection(dto),
                Card card when typeof(TDestination) == typeof(CardDto) =>
                    (TDestination)(object)MapCard(card),
                CardCreateDto dto when typeof(TDestination) == typeof(Card) =>
                    (TDestination)(object)MapCard(dto),
                Word word when typeof(TDestination) == typeof(WordDto) =>
                    (TDestination)(object)MapWord(word),
                WordDto dto when typeof(TDestination) == typeof(Word) =>
                    (TDestination)(object)MapWord(dto),
                WordCreateDto dto when typeof(TDestination) == typeof(Word) =>
                    (TDestination)(object)MapWord(dto),
                WordUpdateDto dto when typeof(TDestination) == typeof(Word) =>
                    (TDestination)(object)MapWord(dto),
                _ => throw new InvalidOperationException($"No mapper registered from {source.GetType().Name} to {typeof(TDestination).Name}.")
            };
        }

        private CollectionDto MapCollection(Collection collection)
        {
            return new CollectionDto
            {
                Id = collection.Id,
                Name = collection.Name,
                Cards = collection.Cards?.Select(MapCard).ToList() ?? [],
                CreatedAt = collection.CreatedAt,
                ModifiedAt = collection.ModifiedAt,
                DeletedAt = collection.DeletedAt
            };
        }

        private static Collection MapCollection(CollectionCreateDto dto)
        {
            return new Collection
            {
                Name = dto.Name,
                CreatedAt = DateTimeOffset.UtcNow,
                Cards = []
            };
        }

        private CardDto MapCard(Card card)
        {
            return new CardDto
            {
                Id = card.Id,
                CollectionId = card.CollectionId,
                Words = card.Words?.Select(MapWord).ToList() ?? [],
                Learnt = card.Learnt,
                DueDate = card.DueDate,
                IntervalDays = card.IntervalDays,
                EaseFactor = card.EaseFactor,
                ReviewCount = card.ReviewCount,
                LastReviewedAt = card.LastReviewedAt
            };
        }

        private Card MapCard(CardCreateDto dto)
        {
            return new Card
            {
                CollectionId = dto.CollectionId,
                Words = dto.Words.Select(MapWord).ToList(),
                CreatedAt = DateTimeOffset.UtcNow,
                DueDate = DateTimeOffset.UtcNow,
                IntervalDays = 0,
                EaseFactor = 2.5m,
                ReviewCount = 0
            };
        }

        private static WordDto MapWord(Word word)
        {
            return new WordDto
            {
                Id = word.Id,
                Value = word.Value,
                Transcription = word.Transcription,
                Translation = word.Translation
            };
        }

        private static Word MapWord(WordDto dto)
        {
            return new Word
            {
                Id = dto.Id,
                Value = dto.Value,
                Transcription = dto.Transcription,
                Translation = dto.Translation
            };
        }

        private static Word MapWord(WordCreateDto dto)
        {
            return new Word
            {
                Value = dto.Value,
                Transcription = dto.Transcription,
                Translation = dto.Translation,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        private static Word MapWord(WordUpdateDto dto)
        {
            return new Word
            {
                Value = dto.Value,
                Transcription = dto.Transcription,
                Translation = dto.Translation,
                ModifiedAt = DateTimeOffset.UtcNow
            };
        }
    }
}

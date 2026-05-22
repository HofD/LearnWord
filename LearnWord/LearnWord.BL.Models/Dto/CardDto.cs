namespace LearnWord.BL.Models.Dto
{
    public class CardDto
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public List<WordDto> Words { get; set; } = [];
        public bool Learnt { get; set; }
        public DateTimeOffset DueDate { get; set; }
        public int IntervalDays { get; set; }
        public decimal EaseFactor { get; set; }
        public int ReviewCount { get; set; }
        public DateTimeOffset? LastReviewedAt { get; set; }
    }
}

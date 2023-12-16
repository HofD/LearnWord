using System;

namespace LearningWords.DAL.Models
{
    public class Word
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public  Card Card { get; set; }
        public string Value { get; set; }
        public string Transcription { get; set; }
        public string Translation { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}

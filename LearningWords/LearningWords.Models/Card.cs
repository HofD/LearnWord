using System;
using System.Collections.Generic;

namespace LearningWords.DAL.Models
{
    public class Card
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public Collection Collection { get; set; }
        public List<Word> Words { get; set; }
        public bool Learnt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ShowedAt { get; set; }
        public DateTimeOffset? LearntAt { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}

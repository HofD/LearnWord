using System;
using System.Collections.Generic;

namespace LearningWords.DAL.Models
{
    public class Collection
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public List<Card> Cards { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}

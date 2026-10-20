using System;
using System.Collections.Generic;

namespace LearnWord.DAL.Models
{
    public class Collection
    {
        public int Id { get; set; }
        public CollectionVisibility Visibility { get; set; } = CollectionVisibility.Private;
        public Guid PublicId { get; set; } = Guid.NewGuid();
        // Deliberately not a foreign key: provenance survives source deletion.
        public int? ForkedFromCollectionId { get; set; }
        public string Name { get; set; }
        public List<Card> Cards { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}

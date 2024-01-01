#nullable disable

using System.ComponentModel.DataAnnotations;

namespace LearningWords.Collections.Identity.DAL.Models
{
    public class CollectionIdentity
    {
        public Guid Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public int CollectionId { get; set; }
    }
}

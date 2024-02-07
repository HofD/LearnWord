#nullable disable

using System.ComponentModel.DataAnnotations;

namespace LearnWord.Identity.DAL.Models
{
    public class CardIdentity
    {
        public Guid Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public int CardId { get; set; }
    }
}

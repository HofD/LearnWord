using LearnWord.BL.Models.Dto;

namespace LearnWord.Review.Models;

public class ReviewCardDto
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public List<WordDto> Words { get; set; }
    public bool Learnt { get; set; }
} 
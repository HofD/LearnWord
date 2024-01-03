namespace LearnWord.BL.Models.Dto
{
    public class CardCreateDto
    {
        public int CollectionId { get; set; }
        public List<WordCreateDto> Words { get; set; }
    }
}

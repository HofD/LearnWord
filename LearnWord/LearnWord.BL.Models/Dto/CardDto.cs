namespace LearnWord.BL.Models.Dto
{
    public class CardDto
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public List<WordDto> Words { get; set; }
        public bool Learnt { get; set; }
    }
}

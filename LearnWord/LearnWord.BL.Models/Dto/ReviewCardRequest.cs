namespace LearnWord.BL.Models.Dto
{
    public class ReviewCardRequest
    {
        public string Outcome { get; set; } = string.Empty;
        public int? ResponseTimeMs { get; set; }
        public Guid? SessionId { get; set; }
        public Guid? RequestId { get; set; }
    }
}

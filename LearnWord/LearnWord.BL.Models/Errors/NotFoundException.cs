namespace LearnWord.BL.Models.Errors
{
    public sealed class NotFoundException : ApiException
    {
        public NotFoundException(string detail, string errorCode = "not_found")
            : base(404, "Not found", errorCode, detail)
        {
        }
    }
}

namespace LearnWord.BL.Models.Errors
{
    public sealed class BadRequestException : ApiException
    {
        public BadRequestException(string detail, string errorCode = "bad_request")
            : base(400, "Bad request", errorCode, detail)
        {
        }
    }
}

namespace LearnWord.BL.Models.Errors
{
    public sealed class ForbiddenException : ApiException
    {
        public ForbiddenException(string detail, string errorCode = "forbidden")
            : base(403, "Forbidden", errorCode, detail)
        {
        }
    }
}

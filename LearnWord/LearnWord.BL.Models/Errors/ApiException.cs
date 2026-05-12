namespace LearnWord.BL.Models.Errors
{
    public abstract class ApiException : Exception
    {
        protected ApiException(int statusCode, string title, string errorCode, string detail)
            : base(detail)
        {
            StatusCode = statusCode;
            Title = title;
            ErrorCode = errorCode;
        }

        public int StatusCode { get; }

        public string Title { get; }

        public string ErrorCode { get; }
    }
}

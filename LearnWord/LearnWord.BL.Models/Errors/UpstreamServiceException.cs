namespace LearnWord.BL.Models.Errors
{
    public sealed class UpstreamServiceException : ApiException
    {
        public UpstreamServiceException(string detail, string errorCode = "upstream_service_error")
            : base(502, "Upstream service error", errorCode, detail)
        {
        }
    }
}

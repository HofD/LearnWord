namespace IdentityService.Authorization.Errors
{
    public sealed class InvalidRefreshTokenException : Exception
    {
        public InvalidRefreshTokenException()
            : base("Invalid token")
        {
        }
    }
}

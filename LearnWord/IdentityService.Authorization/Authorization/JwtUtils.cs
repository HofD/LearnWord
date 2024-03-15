using IdentityService.DAL.Context;
using IdentityService.DAL.Models.Entities;
using System.Security.Cryptography;

namespace IdentityService.Authorization.Authorization
{
    public interface IJwtUtils
    {
        RefreshToken GenerateRefreshToken(string ipAddress);
    }

    public class JwtUtils : IJwtUtils
    {
        private readonly IdentityContext identityContext;

        public JwtUtils(IdentityContext identityContext)
        {
            this.identityContext = identityContext;
        }
        public RefreshToken GenerateRefreshToken(string ipAddress)
        {
            var refreshToken = new RefreshToken
            {
                // token is a cryptographically strong random sequence of values
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64)),
                // token is valid for 7 days
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            // ensure token is unique by checking against db
            var tokenIsUnique = !identityContext.Users.Any(a => a.RefreshTokens.Any(t => t.Token == refreshToken.Token));

            if (!tokenIsUnique)
                return GenerateRefreshToken(ipAddress);

            return refreshToken;
        }
    }
}

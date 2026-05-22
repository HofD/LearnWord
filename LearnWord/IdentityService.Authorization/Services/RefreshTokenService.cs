using IdentityService.Authorization.Authorization;
using IdentityService.Authorization.Errors;
using IdentityService.DAL.Context;
using IdentityService.DAL.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Authorization.Services
{
    public class RefreshTokenService
    {
        private readonly IConfiguration configuration;
        private readonly JwtUtils jwtUtils;
        private readonly IdentityContext context;

        public RefreshTokenService(IConfiguration configuration, JwtUtils jwtUtils, IdentityContext context)
        {
            this.configuration = configuration;
            this.jwtUtils = jwtUtils;
            this.context = context;
        }

        public async Task<RefreshToken> WriteToken(LwIdentityUser user, string ipAddress)
        {
            var refreshToken = jwtUtils.GenerateRefreshToken(ipAddress);
            user.RefreshTokens.Add(refreshToken);

            RemoveOldRefreshTokens(user);

            context.Update(user);
            await context.SaveChangesAsync();

            return refreshToken;
        }

        public void RevokeToken(LwIdentityUser user, string token, string ipAddress)
        {
            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            if (!refreshToken.IsActive)
                throw new InvalidRefreshTokenException();

            // revoke token and save
            RevokeRefreshToken(refreshToken, ipAddress, "Revoked without replacement");
            context.Update(user);
            context.SaveChanges();
        }

        public async Task<RefreshToken> RefreshToken(LwIdentityUser user, string token, string ipAddress)
        {
            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            if (refreshToken.IsRevoked)
            {
                RevokeDescendantRefreshTokens(refreshToken, user, ipAddress, $"Attempted reuse of revoked ancestor token: {token}");
                context.Update(user);
                await context.SaveChangesAsync();
            }

            if (!refreshToken.IsActive)
                throw new InvalidRefreshTokenException();

            var newRefreshToken = RotateRefreshToken(refreshToken, ipAddress);
            user.RefreshTokens.Add(newRefreshToken);

            RemoveOldRefreshTokens(user);

            context.Update(user);
            await context.SaveChangesAsync();

            return newRefreshToken;
        }

        public async Task<LwIdentityUser?> GetUserByRefreshToken(string refreshToken)
        {
            var user = await context.Users.Include(x => x.RefreshTokens)
                .SingleOrDefaultAsync(x => x.RefreshTokens.Any(t => t.Token == refreshToken));

            return user;
        }

        private void RemoveOldRefreshTokens(LwIdentityUser user)
        {
            user.RefreshTokens.RemoveAll(x =>
                !x.IsActive &&
                x.Created.AddDays(Convert.ToDouble(configuration["JwtSettings:RefreshTokenTTL"])) <= DateTime.UtcNow);
        }

        private void RevokeDescendantRefreshTokens(RefreshToken refreshToken, LwIdentityUser user, string ipAddress, string reason)
        {
            // recursively traverse the refresh token chain and ensure all descendants are revoked
            if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
            {
                var childToken = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken.ReplacedByToken);
                if (childToken == null)
                {
                    return;
                }

                if (childToken.IsActive)
                    RevokeRefreshToken(childToken, ipAddress, reason);
                else
                    RevokeDescendantRefreshTokens(childToken, user, ipAddress, reason);
            }
        }

        private void RevokeRefreshToken(RefreshToken token, string ipAddress, string? reason = null, string? replacedByToken = null)
        {
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason;
            token.ReplacedByToken = replacedByToken;
        }

        private RefreshToken RotateRefreshToken(RefreshToken refreshToken, string ipAddress)
        {
            var newRefreshToken = jwtUtils.GenerateRefreshToken(ipAddress);
            RevokeRefreshToken(refreshToken, ipAddress, "Replaced by new token", newRefreshToken.Token);
            return newRefreshToken;
        }
    }
}

using IdentityService.DAL.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityService.Authorization.Authorization
{
    public class JwtHandler
    {
        private readonly IConfigurationSection jwtSettings;
        public JwtHandler(IConfiguration configuration)
        {
            jwtSettings = configuration.GetSection("JwtSettings");
        }
        public SigningCredentials GetSigningCredentials()
        {
            var jwtKey = jwtSettings.GetSection("Key").Value ?? throw new InvalidOperationException("JwtSettings:Key is required.");
            var key = Encoding.UTF8.GetBytes(jwtKey);
            var secret = new SymmetricSecurityKey(key);
            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }
        public List<Claim> GetClaims(LwIdentityUser user)
        {
            var claims = new List<Claim>
        {
            new Claim("Id", user.Id)
        };
            return claims;
        }
        public JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var tokenOptions = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"])),
                signingCredentials: signingCredentials);
            return tokenOptions;
        }
    }
}

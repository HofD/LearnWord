using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace LearnWord.Collections.Identity.Authorization
{
    public class JwtUtils
    {
        private readonly IConfiguration configuration;

        public JwtUtils(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string ValidateJwtToken(string token)
        {
            if (token == null)
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["JwtSettings:Key"]);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(x => x.Type == "Id").Value;

                return userId;
            }
            catch
            {
                return null;
            }
        }
    }
}

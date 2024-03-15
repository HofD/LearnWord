#nullable disable

using Microsoft.AspNetCore.Identity;

namespace IdentityService.DAL.Models.Entities
{
    public class LwIdentityUser : IdentityUser
    {
        public LwIdentityUser(string userName) : base(userName) { }

        public List<RefreshToken> RefreshTokens { get; set; } = [];
    }
}

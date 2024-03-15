#nullable disable

namespace IdentityService.Authorization.Models.Authentication
{
    public class LoginResponse
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}

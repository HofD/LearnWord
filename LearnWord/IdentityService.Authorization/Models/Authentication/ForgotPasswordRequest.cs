#nullable disable

using System.ComponentModel.DataAnnotations;

namespace IdentityService.Authorization.Models.Authentication
{
    public class ForgotPasswordRequest
    {
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; }
    }
}

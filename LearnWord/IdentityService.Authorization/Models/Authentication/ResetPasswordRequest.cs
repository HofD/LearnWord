#nullable disable

using System.ComponentModel.DataAnnotations;

namespace IdentityService.Authorization.Models.Authentication
{
    public class ResetPasswordRequest
    {
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}

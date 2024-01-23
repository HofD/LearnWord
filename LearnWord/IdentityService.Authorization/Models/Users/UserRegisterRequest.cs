#nullable disable

using System.ComponentModel.DataAnnotations;

namespace IdentityService.Authorization.Models.Users
{
    public class UserRegisterRequest
    {
        [Required]
        [MaxLength(100)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}

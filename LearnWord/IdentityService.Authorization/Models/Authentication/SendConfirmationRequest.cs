#nullable disable

using System.ComponentModel.DataAnnotations;

namespace IdentityService.Authorization.Models.Authentication
{
    public class SendConfirmationRequest
    {
        [Required]
        [MaxLength(100)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace MarketConnect.Controllers.Dtos
{
    public class GoogleLoginDto
    {
        [Required]
        public string IdToken { get; set; } = null!;
    }

    public class RegisterPhoneRequestDto
    {
        [Required]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;
    }

    public class VerifyOtpDto
    {
        [Required]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [MaxLength(6)]
        public string OtpCode { get; set; } = null!;
    }
}

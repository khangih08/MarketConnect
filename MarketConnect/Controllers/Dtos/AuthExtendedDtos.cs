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

    public class CheckPhoneDto
    {
        [Required]
        public string PhoneNumber { get; set; } = null!;
    }

    public class PhoneRegisterDto
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string PhoneNumber { get; set; } = null!;

        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = null!;
    }
}

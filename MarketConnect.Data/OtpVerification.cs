using System;
using System.ComponentModel.DataAnnotations;

namespace MarketConnect.Data
{
    public class OtpVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = null!;

        [MaxLength(200)]
        public string? FullName { get; set; }

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        [MaxLength(6)]
        public string OtpCode { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

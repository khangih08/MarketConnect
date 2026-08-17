using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class UserSession
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [MaxLength(255)]
        public string DeviceName { get; set; } = null!;

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(150)]
        public string Location { get; set; } = "Hanoi, Viet Nam";

        public bool IsCurrentSession { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

        public DateTime LastActiveTime { get; set; } = DateTime.UtcNow;
    }
}

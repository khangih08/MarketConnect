using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [MaxLength(50)]
        public string? UserRole { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = null!; // "APPROVE_STORE", "REJECT_PRODUCT", "GRANT_SCOPE", etc.

        [MaxLength(100)]
        public string? EntityName { get; set; }

        public int? EntityId { get; set; }

        public string? DetailsJson { get; set; }

        [MaxLength(64)]
        public string? IpHash { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

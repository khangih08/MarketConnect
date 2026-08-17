using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class ContentVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityName { get; set; } = null!; // "Product", "Store"

        public int EntityId { get; set; }

        public int VersionNumber { get; set; } = 1;

        [Required]
        public string SnapshotJson { get; set; } = null!;

        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

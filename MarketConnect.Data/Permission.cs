using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = null!; // e.g. "CONTENT_APPROVE", "STORE_LOCK"

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string Category { get; set; } = "General"; // "Content", "Store", "User", "Role", "System"

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [ForeignKey(nameof(Permission))]
        public int PermissionId { get; set; }
        public Permission? Permission { get; set; }
    }
}

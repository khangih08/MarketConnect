using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class ProductComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProductId { get; set; } = null!; // ID sản phẩm dạng số hoặc string

        [Required]
        [MaxLength(200)]
        public string UserFullName { get; set; } = null!;

        [MaxLength(500)]
        public string? UserAvatar { get; set; }

        [Required]
        public string CommentText { get; set; } = null!;

        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class CategoryPriceReference
    {
        [Key]
        public int Id { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = "kg"; // e.g., "kg", "hộp", "thùng", "cái", "con"

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MedianPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal P75Price { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

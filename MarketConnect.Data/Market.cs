using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MarketConnect.Data
{
    public class Market
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string Slug { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductMarket>? ProductMarkets { get; set; }
    }
}

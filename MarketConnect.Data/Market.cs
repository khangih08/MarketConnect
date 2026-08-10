using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [ForeignKey(nameof(Province))]
        public int? ProvinceId { get; set; }
        public Province? Province { get; set; }

        [ForeignKey(nameof(District))]
        public int? DistrictId { get; set; }
        public District? District { get; set; }

        [ForeignKey(nameof(Ward))]
        public int? WardId { get; set; }
        public Ward? Ward { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [MaxLength(100)]
        public string? OpeningHours { get; set; } = "05:00 - 19:00";

        [MaxLength(300)]
        public string? ManagementContact { get; set; }

        [MaxLength(500)]
        public string? PopularCategories { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductMarket>? ProductMarkets { get; set; }
        public ICollection<Store>? Stores { get; set; }
    }
}

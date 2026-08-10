using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public enum AdStatus
    {
        Draft,
        PendingApproval,
        Active,
        Paused,
        Expired,
        Rejected
    }

    public class AdPackage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        public int DurationDays { get; set; }

        public int TargetImpressions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [MaxLength(50)]
        public string Position { get; set; } = "SearchTop"; // SearchTop, FeaturedStore, RecommendedProduct, MarketPage

        public bool IsActive { get; set; } = true;
    }

    public class AdCampaign
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Merchant))]
        public int MerchantId { get; set; }
        public User? Merchant { get; set; }

        [ForeignKey(nameof(Store))]
        public int StoreId { get; set; }
        public Store? Store { get; set; }

        [ForeignKey(nameof(Product))]
        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        [ForeignKey(nameof(AdPackage))]
        public int AdPackageId { get; set; }
        public AdPackage? AdPackage { get; set; }

        [ForeignKey(nameof(TargetProvince))]
        public int? TargetProvinceId { get; set; }
        public Province? TargetProvince { get; set; }

        [ForeignKey(nameof(TargetMarket))]
        public int? TargetMarketId { get; set; }
        public Market? TargetMarket { get; set; }

        public string? TargetKeywordsJson { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public AdStatus Status { get; set; } = AdStatus.PendingApproval;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        public int ImpressionsCount { get; set; }
        public int ClicksCount { get; set; }
        public int ContactClicksCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AdEventLog
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(AdCampaign))]
        public int AdCampaignId { get; set; }
        public AdCampaign? AdCampaign { get; set; }

        [Required]
        [MaxLength(30)]
        public string EventType { get; set; } = null!; // "Impression", "Click", "ContactClick"

        [MaxLength(64)]
        public string? IpHash { get; set; }

        [MaxLength(200)]
        public string? DeviceHash { get; set; }

        public bool IsValid { get; set; } = true; // Phát hiện click tặc / gian lận

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class MobileSellerProfile
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [MaxLength(150)]
        public string DisplayName { get; set; } = null!;

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        [Required]
        [MaxLength(100)]
        public string VehicleType { get; set; } = null!; // Xe đạp, Xe máy, Gánh hàng, Quầy di động

        [MaxLength(500)]
        public string ItemsDescription { get; set; } = null!; // "Bánh mì, Xôi nóng, Nước mía"

        [MaxLength(300)]
        public string? PrimaryOperatingArea { get; set; }

        public double DefaultRadiusKm { get; set; } = 3.0;

        public bool IsVerified { get; set; } = false;

        public double ReputationScore { get; set; } = 5.0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SellerAvailability
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User? User { get; set; }

        public bool IsOnline { get; set; } = false;

        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }

        public double ServiceRadiusKm { get; set; } = 3.0;

        public DateTime LastLocationUpdate { get; set; } = DateTime.UtcNow;
    }

    public class LocationSample
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    }

    public enum SellerCallStatus
    {
        SEARCHING,
        OFFERED,
        ACCEPTED,
        APPROACHING,
        ARRIVED,
        COMPLETED,
        NO_SELLER_FOUND,
        EXPIRED,
        CANCELLED_BY_BUYER,
        CANCELLED_BY_SELLER,
        SAFETY_REPORTED
    }

    public class SellerCallRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestCode { get; set; } = null!;

        [ForeignKey(nameof(Buyer))]
        public int BuyerId { get; set; }
        public User? Buyer { get; set; }

        [Required]
        [MaxLength(150)]
        public string TargetItem { get; set; } = null!;

        public double MeetupLatitude { get; set; }
        public double MeetupLongitude { get; set; }

        [MaxLength(300)]
        public string? MeetupAddressNote { get; set; }

        [MaxLength(500)]
        public string? BuyerNote { get; set; }

        public double RadiusKm { get; set; } = 3.0;

        public SellerCallStatus Status { get; set; } = SellerCallStatus.SEARCHING;

        [ForeignKey(nameof(MatchedSeller))]
        public int? MatchedSellerId { get; set; }
        public User? MatchedSeller { get; set; }

        public int? EstimatedArrivalMinutes { get; set; }

        [MaxLength(50)]
        public string? ProtectedContactCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

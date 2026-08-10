using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public enum StoreStatus
    {
        Draft,
        PendingVerification,
        PendingApproval,
        Approved,
        ChangesRequired,
        Rejected,
        Suspended,
        Locked
    }

    public class Store
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Owner))]
        public int UserId { get; set; }
        public User? Owner { get; set; }

        [ForeignKey(nameof(Market))]
        public int MarketId { get; set; }
        public Market? Market { get; set; }

        [Required]
        [MaxLength(200)]
        public string StoreName { get; set; } = null!;

        [MaxLength(200)]
        public string RepresentativeName { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        public string VerifiedPhone { get; set; } = null!;

        [MaxLength(300)]
        public string StallLocation { get; set; } = null!; // Ví dụ: "Quầy 42, Dãy B, Tầng 1"

        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? CoverUrl { get; set; }

        [MaxLength(500)]
        public string? PhotoProofUrl { get; set; } // Ảnh quầy hàng/giấy chứng nhận

        [MaxLength(500)]
        public string? IdentityInfo { get; set; } // Thông tin định danh (CCCD/CMND) lưu bảo mật

        [MaxLength(100)]
        public string? OpeningHours { get; set; } = "06:00 - 18:00";

        // Kênh liên hệ: JSON format {"zalo":"...", "facebook":"...", "phone":"..."}
        public string? ContactChannelsJson { get; set; }

        // Hình thức nhận hàng: "AtStall,SelfDelivery,AgreedDelivery"
        [MaxLength(200)]
        public string PickupMethods { get; set; } = "AtStall,SelfDelivery,AgreedDelivery";

        public StoreStatus Status { get; set; } = StoreStatus.PendingApproval;

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Product>? Products { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<PurchaseRequest>? PurchaseRequests { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public enum ReviewStatus
    {
        Pending,
        Published,
        Reported,
        UnderReview,
        Hidden,
        Removed
    }

    public class Review
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Buyer))]
        public int BuyerId { get; set; }
        public User? Buyer { get; set; }

        [ForeignKey(nameof(Store))]
        public int StoreId { get; set; }
        public Store? Store { get; set; }

        [ForeignKey(nameof(PurchaseRequest))]
        public int? PurchaseRequestId { get; set; }
        public PurchaseRequest? PurchaseRequest { get; set; }

        [Range(1, 5)]
        public int RatingScore { get; set; } // 1 - 5 sao

        // JSON: {"quality": 5, "price": 4, "service": 5, "accuracy": 5}
        public string? CriteriaRatingsJson { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        [MaxLength(1000)]
        public string? MerchantReply { get; set; }
        public DateTime? ReplyUpdatedAt { get; set; }

        public ReviewStatus Status { get; set; } = ReviewStatus.Published;

        public bool IsVerifiedInteraction { get; set; } // Gắn với đơn hàng/tương tác đã xác minh

        public double RatingWeight { get; set; } = 1.0; // Trọng số độ tin cậy

        [MaxLength(64)]
        public string? IpHash { get; set; } // Salted hash IP

        [MaxLength(200)]
        public string? DeviceFingerprint { get; set; }

        public string? EditHistoryJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum AbuseReportStatus
    {
        New,
        UnderReview,
        NeedInfo,
        Handled,
        Appeal,
        Closed
    }

    public class AbuseReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReportCode { get; set; } = null!;

        [ForeignKey(nameof(Reporter))]
        public int ReporterId { get; set; }
        public User? Reporter { get; set; }

        [Required]
        [MaxLength(50)]
        public string TargetType { get; set; } = null!; // "Store", "Product", "Review", "Merchant"

        public int TargetId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ViolationType { get; set; } = null!; // Lừa đảo, Hàng cấm, Đánh giá xúc phạm, Spam

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? EvidenceUrlsJson { get; set; }

        public AbuseReportStatus Status { get; set; } = AbuseReportStatus.New;

        [ForeignKey(nameof(HandlerAdmin))]
        public int? HandlerAdminId { get; set; }
        public User? HandlerAdmin { get; set; }

        [MaxLength(1000)]
        public string? ResolutionNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}

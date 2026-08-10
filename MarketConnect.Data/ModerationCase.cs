using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public enum ModerationDecision
    {
        LowRiskAutoApproved,
        MediumRiskManualQueue,
        HighRiskBlocked,
        AutoRejected
    }

    public enum ModerationStatus
    {
        Draft,
        PendingAutoReview,
        PendingManualReview,
        Approved,
        ChangesRequired,
        Rejected,
        Suspended,
        Archived
    }

    public class ModerationCase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = null!; // "Product", "Store", "Review"

        public int EntityId { get; set; }

        public int RiskScore { get; set; } // 0-100

        public string? TriggeredRulesJson { get; set; } // Chi tiết các rule bị kích hoạt

        public ModerationDecision Decision { get; set; }

        public ModerationStatus Status { get; set; }

        [ForeignKey(nameof(AssignedAdmin))]
        public int? AssignedAdminId { get; set; }
        public User? AssignedAdmin { get; set; }

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }

        public string? ContentSnapshotJson { get; set; } // Phiên bản nội dung khi gửi duyệt

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? HandledAt { get; set; }
    }

    public class ModerationRule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RuleKey { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string RuleName { get; set; } = null!;

        public int Weight { get; set; } // Trọng số điểm rủi ro

        public string? ConfigJson { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

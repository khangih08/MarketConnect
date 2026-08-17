using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

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

        public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

        public string? TriggeredRulesJson { get; set; }

        public string? RuleResultsJson { get; set; }

        public ModerationDecision Decision { get; set; }

        public ModerationStatus Status { get; set; }

        public int CurrentVersionNumber { get; set; } = 1;

        public int? ProvinceId { get; set; }

        public int? MarketId { get; set; }

        public bool IsEscalated { get; set; } = false;

        [MaxLength(1000)]
        public string? EscalatedReason { get; set; }

        [ForeignKey(nameof(AssignedAdmin))]
        public int? AssignedAdminId { get; set; }
        public User? AssignedAdmin { get; set; }

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }

        public string? ContentSnapshotJson { get; set; }

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

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class ModerationActionHistory
    {
        [Key]
        public int Id { get; set; }

        public int CaseId { get; set; }

        public int AdminId { get; set; }

        [ForeignKey(nameof(AdminId))]
        public virtual User? Admin { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = null!; // "Approve", "Reject", "RequestEdit", "Hide", "Escalate", "Override", "BulkApprove", "BulkReject"

        [MaxLength(50)]
        public string? OldStatus { get; set; }

        [MaxLength(50)]
        public string? NewStatus { get; set; }

        [MaxLength(50)]
        public string? OldDecision { get; set; }

        [MaxLength(50)]
        public string? NewDecision { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = null!;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public enum ModerationAppealStatus
    {
        Pending,
        UnderReview,
        Accepted,
        Rejected
    }

    public class ModerationAppeal
    {
        [Key]
        public int Id { get; set; }

        public int CaseId { get; set; }

        [ForeignKey(nameof(CaseId))]
        public virtual ModerationCase? ModerationCase { get; set; }

        public int MerchantId { get; set; }

        [ForeignKey(nameof(MerchantId))]
        public virtual User? Merchant { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Reason { get; set; } = null!;

        public ModerationAppealStatus Status { get; set; } = ModerationAppealStatus.Pending;

        [MaxLength(1000)]
        public string? AdminResponse { get; set; }

        public int? HandledByAdminId { get; set; }

        [ForeignKey(nameof(HandledByAdminId))]
        public virtual User? HandledByAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? HandledAt { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public enum ScopeLevel
    {
        System,
        Province,
        Market,
        Moderator
    }

    public class AdminScope
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User? User { get; set; }

        public ScopeLevel ScopeLevel { get; set; }

        [ForeignKey(nameof(Province))]
        public int? ProvinceId { get; set; }
        public Province? Province { get; set; }

        [ForeignKey(nameof(Market))]
        public int? MarketId { get; set; }
        public Market? Market { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}

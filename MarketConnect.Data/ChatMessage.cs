using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Sender))]
        public int SenderId { get; set; }

        [ForeignKey(nameof(Receiver))]
        public int ReceiverId { get; set; }

        [Required]
        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        [InverseProperty(nameof(User.SentMessages))]
        public User? Sender { get; set; }

        [InverseProperty(nameof(User.ReceivedMessages))]
        public User? Receiver { get; set; }
    }
}

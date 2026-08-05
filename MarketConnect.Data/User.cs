using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = null!;

        [MaxLength(200)]
        public string? Name { get; set; }

        [Phone]
        [MaxLength(30)]
        public string? Phone { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [MaxLength(500)] 
        public string? Address { get; set; }

        [MaxLength(20)] 
        public string? Gender { get; set; }

        [DataType(DataType.Date)] 
        public DateTime? DateOfBirth { get; set; }

        // Navigation property for listings posted by user
        [InverseProperty(nameof(Product.Seller))]
        public ICollection<Product>? Products { get; set; }

        [InverseProperty(nameof(Wishlist.Buyer))]
        public ICollection<Wishlist>? Wishlists { get; set; }

        [InverseProperty(nameof(ChatMessage.Sender))]
        public ICollection<ChatMessage>? SentMessages { get; set; }

        [InverseProperty(nameof(ChatMessage.Receiver))]
        public ICollection<ChatMessage>? ReceivedMessages { get; set; }
    }

    public enum UserRole
    {
        Buyer, Merchant
    }
}

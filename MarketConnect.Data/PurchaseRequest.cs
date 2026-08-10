using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Buyer))]
        public int BuyerId { get; set; }
        public User? Buyer { get; set; }

        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [ForeignKey(nameof(Store))]
        public int StoreId { get; set; }
        public Store? Store { get; set; }

        public int Quantity { get; set; } = 1;

        [MaxLength(200)]
        public string? SelectedOptions { get; set; } // Phân loại (Size, loại...)

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum PurchaseRequestStatus
    {
        Sent,
        Viewed,
        Contacting,
        Confirmed,
        Completed,
        CancelledByBuyer,
        DeclinedByMerchant,
        NoResponse
    }

    public class PurchaseRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestCode { get; set; } = null!; // Mã yêu cầu (VD: PR-20260807-XXXX)

        [ForeignKey(nameof(Buyer))]
        public int BuyerId { get; set; }
        public User? Buyer { get; set; }

        [ForeignKey(nameof(Store))]
        public int StoreId { get; set; }
        public Store? Store { get; set; }

        public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Sent;

        [Required]
        [MaxLength(100)]
        public string BuyerName { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        public string BuyerPhone { get; set; } = null!;

        [MaxLength(200)]
        public string? PreferredPickupMethod { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReferenceTotalPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PurchaseRequestItem>? Items { get; set; }
    }

    public class PurchaseRequestItem
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(PurchaseRequest))]
        public int PurchaseRequestId { get; set; }
        public PurchaseRequest? PurchaseRequest { get; set; }

        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        [MaxLength(200)]
        public string? OptionsNote { get; set; }
    }
}

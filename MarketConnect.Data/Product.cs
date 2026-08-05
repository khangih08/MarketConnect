using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!; // Tiêu đề tin đăng

        public string? Description { get; set; } // Mô tả tin đăng

        [MaxLength(1000)]
        public string? ImageUrl { get; set; } // Hình ảnh / Video đại diện

        public string? MediaUrls { get; set; } // Danh sách ảnh/video bổ sung (phân cách bằng dấu phẩy)

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Giá bán

        public bool IsFree { get; set; } // Cho tặng miễn phí

        [MaxLength(300)]
        public string? Address { get; set; } // Địa chỉ

        [MaxLength(50)]
        public string? SellerType { get; set; } // "Cá nhân" hoặc "Bán chuyên"

        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        [MaxLength(100)]
        public string? Condition { get; set; } // Tình trạng (Mới, Đã sử dụng...)

        [MaxLength(100)]
        public string? SubCategory { get; set; } // Loại phụ kiện

        [MaxLength(100)]
        public string? Origin { get; set; } // Xuất xứ

        [MaxLength(200)]
        public string? Warranty { get; set; } // Chính sách bảo hành

        [ForeignKey(nameof(Seller))]
        public int? UserId { get; set; } // ID người đăng tin

        public User? Seller { get; set; }

        [InverseProperty(nameof(Wishlist.Product))]
        public ICollection<Wishlist>? Wishlists { get; set; }

        public ICollection<ProductMarket>? ProductMarkets { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

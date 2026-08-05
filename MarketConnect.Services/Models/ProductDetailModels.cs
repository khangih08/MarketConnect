using System;
using System.Collections.Generic;

namespace MarketConnect.Services
{
    public class ProductDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string GroupKey { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<string> GalleryImages { get; set; } = new List<string>();
        public double Price { get; set; }
        public bool IsFree { get; set; }
        public string Address { get; set; } = string.Empty;
        public string SellerType { get; set; } = "Cá nhân";
        public string Condition { get; set; } = "Đã sử dụng";
        public string SubCategory { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int SoldCount { get; set; }
        public double Rating { get; set; }
        public bool IsBestSeller { get; set; }
        public int DiscountPercent { get; set; }
        public Dictionary<string, string> Specifications { get; set; } = new Dictionary<string, string>();
        public SellerInfoDto SellerInfo { get; set; } = new SellerInfoDto();
        public List<ProductCommentDto> Comments { get; set; } = new List<ProductCommentDto>();
    }

    public class SellerInfoDto
    {
        public string SellerId { get; set; } = "1";
        public string SellerName { get; set; } = "Khôi Nguyễn";
        public string SellerAvatar { get; set; } = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150";
        public string SellerType { get; set; } = "Cá nhân";
        public double Rating { get; set; } = 4.9;
        public int TotalProducts { get; set; } = 12;
        public bool IsOnline { get; set; } = true;
        public string LastActive { get; set; } = "5 phút trước";
        public string Phone { get; set; } = "0988 123 456";
        public string Address { get; set; } = "Quận Ba Đình, Hà Nội";
    }

    public class ProductCreateDto
    {
        public string Title { get; set; } = null!;
        public double Price { get; set; }
        public bool IsFree { get; set; }
        public string Address { get; set; } = null!;
        public string? MarketName { get; set; } // Tên chợ đăng tin (vd: "Chợ Đồng Xuân")
        public List<int>? MarketIds { get; set; } // Danh sách ID chợ đăng tin (Multi-market)
        public string SellerType { get; set; } = "Cá nhân"; // "Cá nhân" hoặc "Bán chuyên"
        public int CategoryId { get; set; }
        public string Condition { get; set; } = null!; // "Đã sử dụng (chưa sửa chữa)", "Mới 100%"...
        public string? SubCategory { get; set; } // Loại phụ kiện
        public string? Origin { get; set; } // Xuất xứ
        public string? Warranty { get; set; } // Chính sách bảo hành
        public string? ImageUrl { get; set; } // Hình ảnh/video chính
        public string? MediaUrls { get; set; } // Các ảnh/video khác
        public string? Description { get; set; } // Mô tả tin đăng
    }

    public class ProductCommentDto
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserAvatar { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }

    public class CreateCommentDto
    {
        public string CommentText { get; set; } = string.Empty;
        public string UserFullName { get; set; } = "Khách hàng";
    }

    public class RelatedProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string GroupKey { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsFree { get; set; }
        public string Address { get; set; } = string.Empty;
        public double? OriginalPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public int? DiscountPercent { get; set; }
    }
}

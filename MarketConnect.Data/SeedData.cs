using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext db)
        {
            // 0. Đảm bảo bảng ProductComments được tạo tự động trong PostgreSQL Database nếu chưa có
            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""ProductComments"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""ProductId"" VARCHAR(100) NOT NULL,
                        ""UserFullName"" VARCHAR(200) NOT NULL,
                        ""UserAvatar"" VARCHAR(500),
                        ""CommentText"" TEXT NOT NULL,
                        ""UserId"" INT NULL,
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                    );
                ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeedData] ProductComments table init notice: {ex.Message}");
            }

            var targetCategories = new List<string>

            {
                "Rau củ & Trái cây tươi",
                "Thịt & Gia cầm",
                "Thủy hải sản tươi sống",
                "Thực phẩm chế biến sẵn",
                "Ẩm thực & Chợ ăn vặt",
                "Gia vị & Đồ khô",
                "Gạo, Nếp & Ngũ cốc",
                "Trứng & Sữa",
                "Đồ gia dụng & Tiện ích",
                "Hoa tươi & Đồ thờ cúng"
            };

            // 1. Thêm các danh mục Chợ Nông sản & Thực phẩm chuẩn nếu chưa có trong DB
            foreach (var catName in targetCategories)
            {
                var exists = await db.Categories.AnyAsync(c => c.Name.ToLower() == catName.ToLower());
                if (!exists)
                {
                    db.Categories.Add(new Category { Name = catName });
                }
            }
            await db.SaveChangesAsync();

            // Lấy danh mục mặc định đầu tiên để chuyển hướng các sản phẩm thuộc danh mục cũ
            var defaultCat = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Rau củ & Trái cây tươi") 
                ?? await db.Categories.FirstAsync();

            // 2. Xóa hoặc chuyển đổi các danh mục cũ không liên quan (Electronics, Books, Home, ...)
            var oldLegacyNames = new[] { "electronics", "books", "home" };
            var legacyCategories = await db.Categories
                .Where(c => oldLegacyNames.Contains(c.Name.ToLower()))
                .ToListAsync();

            if (legacyCategories.Any())
            {
                foreach (var oldCat in legacyCategories)
                {
                    var productsWithOldCat = await db.Products.Where(p => p.CategoryId == oldCat.Id).ToListAsync();
                    foreach (var p in productsWithOldCat)
                    {
                        p.CategoryId = defaultCat.Id;
                    }
                    db.Categories.Remove(oldCat);
                }
                await db.SaveChangesAsync();
            }

            // 3. Khởi tạo tài khoản người dùng mặc định nếu chưa có
            var defaultUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "user@marketconnect.vn");
            if (defaultUser == null)
            {
                defaultUser = new User
                {
                    Email = "user@marketconnect.vn",
                    PasswordHash = "hashed_password",
                    Name = "Khôi Nguyễn",
                    Phone = "0988 123 456",
                    Role = UserRole.Buyer,
                    Address = "Quận Ba Đình, Hà Nội"
                };

                db.Users.Add(defaultUser);
                await db.SaveChangesAsync();
            }

            // 4. Khởi tạo dữ liệu sản phẩm mẫu nếu chưa có
            if (!await db.Products.AnyAsync())
            {
                var catProduce = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Rau củ & Trái cây tươi") ?? defaultCat;
                var catMeat = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Thịt & Gia cầm") ?? defaultCat;
                var catSeafood = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Thủy hải sản tươi sống") ?? defaultCat;

                db.Products.AddRange(
                   new Product 
                   { 
                       Name = "Táo Envy Mỹ Nhập Khẩu Tươi Giòn Ngọt", 
                       Description = "Táo Envy nhập khẩu trực tiếp từ Mỹ, quả to tròn, thịt giòn ngọt, mọng nước, nhiều dưỡng chất.", 
                       ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600",
                       Price = 120000,
                       IsFree = false,
                       Address = "Chợ Đồng Xuân, Hà Nội",
                       SellerType = "Chợ truyền thống",
                       CategoryId = catProduce.Id,
                       Condition = "Tươi sống mới về",
                       SubCategory = "Trái cây nhập khẩu",
                       Origin = "Mỹ",
                       Warranty = "Đảm bảo độ tươi ngon 100%",
                       UserId = defaultUser.Id,
                       CreatedAt = DateTime.Now
                   },
                   new Product 
                   { 
                       Name = "Thịt Bò Mỹ Ba Chỉ Nhập Khẩu Cuộn Lẩu", 
                       Description = "Thịt ba chỉ bò Mỹ cuộn mỏng vừa ăn, vân mỡ đều, mềm mọng thích hợp cho món lẩu và nướng gia đình.", 
                       ImageUrl = "https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=600",
                       Price = 185000,
                       IsFree = false,
                       Address = "Chợ Thành Công, Hà Nội",
                       SellerType = "Tiểu thương chợ",
                       CategoryId = catMeat.Id,
                       Condition = "Tươi lạnh bảo quản chuẩn",
                       SubCategory = "Thịt bò nhập khẩu",
                       Origin = "Mỹ",
                       Warranty = "Hạn sử dụng trong ngày",
                       UserId = defaultUser.Id,
                       CreatedAt = DateTime.Now
                   },
                   new Product 
                   { 
                       Name = "Cá Hồi Na Uy Tươi Sống Nguyên Con / Cắt Khúc", 
                       Description = "Cá hồi Na Uy phi lê tươi trong ngày, thịt màu cam tươi, béo ngậy giàu Omega-3, thích hợp làm Sashimi hoặc áp chảo.", 
                       ImageUrl = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=600",
                       Price = 350000,
                       IsFree = false,
                       Address = "Chợ Long Biên, Hà Nội",
                       SellerType = "Tiểu thương chợ",
                       CategoryId = catSeafood.Id,
                       Condition = "Tươi sống 100%",
                       SubCategory = "Hải sản cao cấp",
                       Origin = "Na Uy",
                       Warranty = "Cấp đông tiêu chuẩn export",
                       UserId = defaultUser.Id,
                       CreatedAt = DateTime.Now
                   }
                );

                await db.SaveChangesAsync();
            }
        }
    }
}


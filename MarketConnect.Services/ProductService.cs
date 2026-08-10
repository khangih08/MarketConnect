using Elastic.Clients.Elasticsearch;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketConnect.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly ElasticsearchClient _elasticClient;
        private static readonly ConcurrentDictionary<string, List<ProductCommentDto>> _commentsStore = new();

        public ProductService(ApplicationDbContext db, ElasticsearchClient elasticClient)
        {
            _db = db;
            _elasticClient = elasticClient;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            if (product.CategoryId <= 0)
            {
                if (product.Category != null && product.Category.Id > 0)
                {
                    product.CategoryId = product.Category.Id;
                    _db.Categories.Attach(product.Category);
                    product.Category = null;
                }
                else
                {
                    throw new System.ArgumentException("Product must reference an existing CategoryId or Category.Id > 0");
                }
            }

            var exists = await _db.Categories.AnyAsync(c => c.Id == product.CategoryId);
            if (!exists) throw new System.ArgumentException($"Category with Id {product.CategoryId} does not exist.");

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product;
        }

        public async Task<Product> CreateListingAsync(ProductCreateDto dto, int? userId = null)
        {
            if (dto.CategoryId <= 0)
            {
                var firstCat = await _db.Categories.FirstOrDefaultAsync();
                dto.CategoryId = firstCat?.Id ?? 1;
            }

            int? storeId = null;
            Store? approvedStore = null;
            if (userId.HasValue && userId.Value > 0)
            {
                approvedStore = await _db.Stores
                    .Include(s => s.Market)
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.Status == StoreStatus.Approved);

                if (approvedStore != null)
                {
                    storeId = approvedStore.Id;
                }
            }

            var product = new Product
            {
                Name = dto.Title,
                Price = dto.IsFree ? 0 : (decimal)dto.Price,
                IsFree = dto.IsFree,
                Address = dto.Address,
                SellerType = approvedStore != null ? "Bán chuyên" : (dto.SellerType ?? "Cá nhân"),
                CategoryId = dto.CategoryId,
                Condition = dto.Condition,
                SubCategory = dto.SubCategory,
                Origin = dto.Origin,
                Warranty = dto.Warranty,
                ImageUrl = !string.IsNullOrWhiteSpace(dto.ImageUrl) ? dto.ImageUrl : "/images/seed/headphones.svg",
                MediaUrls = dto.MediaUrls,
                Description = dto.Description,
                UserId = userId,
                StoreId = storeId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            // Gán sản phẩm vào Chợ (Multi-market association)
            try
            {
                var marketNamesToAssign = new List<string>();

                if (approvedStore != null && approvedStore.Market != null && !string.IsNullOrWhiteSpace(approvedStore.Market.Name))
                {
                    // Ưu tiên gán sản phẩm vào CHÍNH XÁC Chợ mà gian hàng tiểu thương đã đăng ký
                    marketNamesToAssign.Add(approvedStore.Market.Name);
                }
                else if (!string.IsNullOrWhiteSpace(dto.MarketName))
                {
                    marketNamesToAssign.AddRange(dto.MarketName.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim()));
                }
                else
                {
                    marketNamesToAssign.Add("Chợ Đồng Xuân"); // Mặc định
                }

                foreach (var mName in marketNamesToAssign.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(mName)) continue;

                    var market = await _db.Markets.FirstOrDefaultAsync(m => m.Name == mName || m.Name.ToLower() == mName.ToLower());
                    if (market == null)
                    {
                        market = new Market
                        {
                            Name = mName,
                            Slug = mName.ToLower().Replace(" ", "-")
                        };
                        _db.Markets.Add(market);
                        await _db.SaveChangesAsync();
                    }

                    var exists = await _db.ProductMarkets.AnyAsync(pm => pm.MarketId == market.Id && pm.ProductId == product.Id);
                    if (!exists)
                    {
                        _db.ProductMarkets.Add(new ProductMarket
                        {
                            MarketId = market.Id,
                            ProductId = product.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MultiMarket Association Notice] {ex.Message}");
            }

            return product;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.Products.FindAsync(id);
            if (existing == null) return false;
            _db.Products.Remove(existing);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            try
            {
                return await _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .AsNoTracking()
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductService] Error in GetAllAsync: {ex.Message}");
                return new List<Product>();
            }
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            try
            {
                return await _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductService] Error in GetByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return await _db.Categories.AsNoTracking().ToListAsync();
        }

        public async Task<Product?> UpdateAsync(int id, Product product)
        {
            var existing = await _db.Products.FindAsync(id);
            if (existing == null) return null;

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.ImageUrl = product.ImageUrl;
            existing.Price = product.Price;
            existing.Address = product.Address;
            existing.Condition = product.Condition;
            existing.CategoryId = product.CategoryId;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<ProductDocument?> GetProductByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var response = await _elasticClient.GetAsync<ProductDocument>(id, g => g.Index("products"));

            if (response.IsValidResponse && response.Source != null)
            {
                return response.Source;
            }

            return null;
        }

        public async Task<ProductDocument?> GetById(string id)
        {
            return await GetProductByIdAsync(id);
        }

        public async Task<ProductDetailDto?> GetProductDetailAsync(string id)
        {
            Product? dbProduct = null;
            if (int.TryParse(id, out int numericId))
            {
                dbProduct = await GetByIdAsync(numericId);
            }
            else
            {
                dbProduct = await _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Name != null && p.Name.ToLower() == id.ToLower());
            }

            if (dbProduct != null)
            {
                double realPrice = (double)dbProduct.Price;
                string mainImage = !string.IsNullOrEmpty(dbProduct.ImageUrl) 
                    ? dbProduct.ImageUrl 
                    : "/images/seed/headphones.svg";

                var comments = await GetCommentsAsync(dbProduct.Id.ToString());

                var gallery = new List<string> { mainImage };
                if (!string.IsNullOrWhiteSpace(dbProduct.MediaUrls))
                {
                    var extraUrls = dbProduct.MediaUrls.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var url in extraUrls)
                    {
                        if (!gallery.Contains(url.Trim())) gallery.Add(url.Trim());
                    }
                }

                var specs = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(dbProduct.Condition)) specs["Tình trạng"] = dbProduct.Condition;
                if (!string.IsNullOrEmpty(dbProduct.SubCategory)) specs["Loại phụ kiện"] = dbProduct.SubCategory;
                if (!string.IsNullOrEmpty(dbProduct.Origin)) specs["Xuất xứ"] = dbProduct.Origin;
                if (!string.IsNullOrEmpty(dbProduct.Warranty)) specs["Chính sách bảo hành"] = dbProduct.Warranty;
                if (!string.IsNullOrEmpty(dbProduct.Address)) specs["Địa điểm giao dịch"] = dbProduct.Address;

                if (specs.Count == 0)
                {
                    specs = GetSpecificationsForProduct(dbProduct.Name, dbProduct.Category?.Name);
                }

                // Tìm chính xác tài khoản người đăng tin trong DB
                var sellerUser = dbProduct.Seller;
                if (sellerUser == null && dbProduct.UserId.HasValue)
                {
                    sellerUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == dbProduct.UserId.Value);
                }
                if (sellerUser == null)
                {
                    sellerUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync();
                }

                // 1. Tên người dùng chính xác
                string realSellerName = "Người dùng MarketConnect";
                if (sellerUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(sellerUser.Name))
                        realSellerName = sellerUser.Name;
                    else if (!string.IsNullOrWhiteSpace(sellerUser.Email))
                        realSellerName = sellerUser.Email.Split('@')[0];
                    else if (!string.IsNullOrWhiteSpace(sellerUser.Phone))
                        realSellerName = sellerUser.Phone;
                }

                // 2. Tổng số tin đã đăng của người dùng này trong Database
                int totalSellerProducts = sellerUser != null 
                    ? await _db.Products.AsNoTracking().CountAsync(p => p.UserId == sellerUser.Id) 
                    : 1;
                totalSellerProducts = Math.Max(totalSellerProducts, 1);

                // 3. Thời gian hoạt động gần nhất
                string lastActiveText = "Đang hoạt động";
                bool isOnline = true;
                var latestProduct = sellerUser != null 
                    ? await _db.Products.AsNoTracking().Where(p => p.UserId == sellerUser.Id).OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync() 
                    : dbProduct;

                if (latestProduct != null)
                {
                    var timeDiff = DateTime.Now - latestProduct.CreatedAt;
                    if (timeDiff.TotalMinutes < 15)
                    {
                        lastActiveText = "Đang hoạt động";
                        isOnline = true;
                    }
                    else if (timeDiff.TotalHours < 1)
                    {
                        lastActiveText = $"Hoạt động {Math.Max(1, (int)timeDiff.TotalMinutes)} phút trước";
                        isOnline = true;
                    }
                    else if (timeDiff.TotalDays < 1)
                    {
                        lastActiveText = $"Hoạt động {Math.Max(1, (int)timeDiff.TotalHours)} giờ trước";
                        isOnline = false;
                    }
                    else
                    {
                        lastActiveText = $"Hoạt động {Math.Max(1, (int)timeDiff.TotalDays)} ngày trước";
                        isOnline = false;
                    }
                }

                // 4. Đánh giá tính toán động dựa trên số bình luận thực tế
                double calculatedRating = 5.0;
                if (comments != null && comments.Count > 0)
                {
                    calculatedRating = Math.Round(Math.Min(5.0, 4.5 + (comments.Count * 0.1)), 1);
                }

                string phone = sellerUser?.Phone ?? "Chưa cập nhật SĐT";

                return new ProductDetailDto
                {
                    Id = dbProduct.Id.ToString(),
                    ProductName = dbProduct.Name,
                    GroupKey = dbProduct.Name.ToLower().Replace(" ", "-"),
                    Brand = dbProduct.Category?.Name ?? "Nông sản & Thực phẩm",
                    Description = !string.IsNullOrEmpty(dbProduct.Description)
                        ? dbProduct.Description
                        : $"Sản phẩm <strong>{dbProduct.Name}</strong> đăng bán trực tiếp trên MarketConnect.",
                    ImageUrl = mainImage,
                    GalleryImages = gallery,
                    Price = realPrice,
                    IsFree = dbProduct.IsFree,
                    Address = dbProduct.Address ?? "Hà Nội",
                    SellerType = dbProduct.SellerType ?? "Cá nhân",
                    Condition = dbProduct.Condition ?? "Tươi mới về trong ngày",
                    SubCategory = dbProduct.SubCategory ?? "",
                    Origin = dbProduct.Origin ?? "",
                    Warranty = dbProduct.Warranty ?? "",
                    CategoryId = dbProduct.CategoryId,
                    CategoryName = dbProduct.Category?.Name ?? "Rau củ & Trái cây tươi",
                    SoldCount = 1,
                    Rating = calculatedRating,
                    IsBestSeller = false,
                    DiscountPercent = 0,
                    Specifications = specs,
                    SellerInfo = new SellerInfoDto
                    {
                        SellerId = sellerUser?.Id.ToString() ?? "1",
                        SellerName = realSellerName,
                        SellerAvatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150",
                        SellerType = dbProduct.SellerType ?? "Cá nhân",
                        Rating = calculatedRating,
                        TotalProducts = totalSellerProducts,
                        IsOnline = isOnline,
                        LastActive = lastActiveText,
                        Phone = phone,
                        Address = dbProduct.Address ?? sellerUser?.Address ?? "Hà Nội"
                    },
                    Comments = comments ?? new List<ProductCommentDto>()
                };
            }

            // Fallback to Elasticsearch document if DB record is absent
            var doc = await GetProductByIdAsync(id);
            if (doc != null)
            {
                string mainImage = !string.IsNullOrEmpty(doc.ImageUrl) ? doc.ImageUrl : "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=600";
                var comments = await GetCommentsAsync(doc.Id ?? id);

                User? sellerUser = null;
                if (!string.IsNullOrEmpty(doc.ShopId) && int.TryParse(doc.ShopId, out int shopUserId))
                {
                    sellerUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == shopUserId);
                }
                if (sellerUser == null)
                {
                    sellerUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync();
                }

                string realSellerName = sellerUser?.Name 
                    ?? (sellerUser?.Email != null ? sellerUser.Email.Split('@')[0] : null) 
                    ?? doc.ShopName 
                    ?? "Người bán MarketConnect";

                int totalSellerProducts = sellerUser != null 
                    ? await _db.Products.AsNoTracking().CountAsync(p => p.UserId == sellerUser.Id) 
                    : 1;

                return new ProductDetailDto
                {
                    Id = doc.Id ?? id,
                    ProductName = doc.ProductName ?? "Tin đăng MarketConnect",
                    GroupKey = doc.GroupKey ?? "default-group",
                    Brand = !string.IsNullOrEmpty(doc.Brand) ? doc.Brand : "Cá nhân",
                    Description = !string.IsNullOrEmpty(doc.Description) ? doc.Description : $"Sản phẩm <strong>{doc.ProductName}</strong> đăng bán trực tiếp.",
                    ImageUrl = mainImage,
                    GalleryImages = doc.GalleryImages ?? new List<string> { mainImage },
                    Price = doc.Price > 0 ? doc.Price : 6790000,
                    IsFree = false,
                    Address = doc.Province ?? "Hà Nội",
                    SellerType = "Cá nhân",
                    Condition = "Đã sử dụng",
                    SoldCount = 1,
                    Rating = doc.Rating > 0 ? doc.Rating : 4.8,
                    IsBestSeller = false,
                    DiscountPercent = 0,
                    Specifications = doc.Specifications ?? GetSpecificationsForProduct(doc.ProductName ?? "", "Điện tử"),
                    SellerInfo = new SellerInfoDto
                    {
                        SellerId = sellerUser?.Id.ToString() ?? doc.ShopId ?? "1",
                        SellerName = realSellerName,
                        SellerAvatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150",
                        SellerType = "Cá nhân",
                        Rating = doc.ShopRating > 0 ? doc.ShopRating : 4.9,
                        TotalProducts = Math.Max(totalSellerProducts, 1),
                        IsOnline = true,
                        LastActive = "Đang hoạt động",
                        Phone = sellerUser?.Phone ?? doc.ShopPhone ?? "Chưa cập nhật SĐT",
                        Address = sellerUser?.Address ?? doc.Province ?? "Hà Nội"
                    },
                    Comments = comments ?? new List<ProductCommentDto>()
                };
            }

            return null;
        }

        public async Task<List<RelatedProductDto>> GetRelatedProductsAsync(string id, int count = 5)
        {
            var result = new List<RelatedProductDto>();
            try
            {
                Product? currentProduct = null;
                if (int.TryParse(id, out int numericId) && numericId > 0)
                {
                    currentProduct = await _db.Products.Include(p => p.Category).AsNoTracking().FirstOrDefaultAsync(p => p.Id == numericId);
                }
                else if (!string.IsNullOrWhiteSpace(id))
                {
                    var lowerId = id.ToLower();
                    currentProduct = await _db.Products.Include(p => p.Category).AsNoTracking()
                        .FirstOrDefaultAsync(p => (p.Name != null && p.Name.ToLower().Contains(lowerId)));
                }

                int categoryId = currentProduct?.CategoryId ?? 0;
                string categoryName = currentProduct?.Category?.Name ?? "";

                var query = _db.Products.Include(p => p.Category).AsNoTracking().AsQueryable();
                if (currentProduct != null)
                {
                    query = query.Where(p => p.Id != currentProduct.Id);
                }

                if (categoryId > 0)
                {
                    query = query.Where(p => p.CategoryId == categoryId)
                                 .OrderByDescending(p => p.CreatedAt);
                }
                else if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    query = query.Where(p => p.Category != null && p.Category.Name.ToLower() == categoryName.ToLower())
                                 .OrderByDescending(p => p.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }

                var dbProducts = await query.Take(count).ToListAsync();

                // Nếu không đủ sản phẩm trong danh mục, bổ sung thêm từ toàn bộ DB
                if (dbProducts.Count < count)
                {
                    var existingIds = dbProducts.Select(p => p.Id).ToList();
                    if (currentProduct != null) existingIds.Add(currentProduct.Id);

                    var extraProducts = await _db.Products.Include(p => p.Category).AsNoTracking()
                        .Where(p => !existingIds.Contains(p.Id))
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(count - dbProducts.Count)
                        .ToListAsync();

                    dbProducts.AddRange(extraProducts);
                }

                if (dbProducts.Any())
                {
                    foreach (var p in dbProducts)
                    {
                        double price = (double)p.Price;
                        string title = !string.IsNullOrEmpty(p.Name) ? p.Name : "Sản phẩm Nông sản";
                        string img = !string.IsNullOrEmpty(p.ImageUrl) && !p.ImageUrl.Contains("placeholder") 
                            ? p.ImageUrl 
                            : "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600";

                        result.Add(new RelatedProductDto
                        {
                            Id = p.Id.ToString(),
                            GroupKey = title.ToLower().Replace(" ", "-"),
                            Brand = p.Category?.Name ?? "Nông sản & Thực phẩm",
                            ProductName = title,
                            Price = price,
                            IsFree = p.IsFree,
                            Address = p.Address ?? "Chợ Đồng Xuân (Hà Nội)",
                            OriginalPrice = price > 0 ? price * 1.15 : 0,
                            DiscountPercent = 0,
                            Rating = 4.9,
                            ReviewCount = 12,
                            ImageUrl = img
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductService] Error in GetRelatedProductsAsync: {ex.Message}");
            }

            // Bổ sung các sản phẩm Chợ Nông sản & Thực phẩm chuẩn thực tế nếu DB vẫn chưa đủ
            var produceFallbacks = new List<RelatedProductDto>
            {
                new RelatedProductDto { Id = "2", GroupKey = "cam-sanh-tien-giang", Brand = "Rau củ & Trái cây tươi", ProductName = "Cam Sành Tiền Giang Mọng Nước Ngọt Thanh 2kg", Price = 45000, Address = "Chợ Đồng Xuân (Hà Nội)", Rating = 4.8, ReviewCount = 18, ImageUrl = "https://images.unsplash.com/photo-1611080626919-7cf5a9dbab5b?w=600" },
                new RelatedProductDto { Id = "3", GroupKey = "rau-cai-thao-da-lat", Brand = "Rau củ & Trái cây tươi", ProductName = "Rau Cải Thảo Đà Lạt Hữu Cơ Sạch 1kg", Price = 25000, Address = "Chợ Long Biên (Hà Nội)", Rating = 4.7, ReviewCount = 10, ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=600" },
                new RelatedProductDto { Id = "4", GroupKey = "thit-than-bo-wagyu", Brand = "Thịt & Gia cầm", ProductName = "Thịt Thăn Bò Wagyu Úc MB 4-5 - Gói 500g Tiêu Chuẩn", Price = 450000, Address = "Chợ Hàng Bè (Hà Nội)", Rating = 5.0, ReviewCount = 24, ImageUrl = "https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=600" },
                new RelatedProductDto { Id = "5", GroupKey = "ga-ta-tha-vuon", Brand = "Thịt & Gia cầm", ProductName = "Gà Ta Thả Vườn Nguyên Con Tươi Ngon Cấp Sạch", Price = 185000, Address = "Chợ Mơ (Hà Nội)", Rating = 4.8, ReviewCount = 15, ImageUrl = "https://images.unsplash.com/photo-1587593810167-a84920ea0781?w=600" },
                new RelatedProductDto { Id = "7", GroupKey = "ca-hoi-na-uy", Brand = "Thủy hải sản tươi sống", ProductName = "Cá Hồi Na Uy Tươi Sống Phi Lê Cắt Khúc 300g", Price = 350000, Address = "Chợ Hàng Bè (Hà Nội)", Rating = 4.9, ReviewCount = 30, ImageUrl = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=600" },
                new RelatedProductDto { Id = "10", GroupKey = "cha-lua-uoc-le", Brand = "Thực phẩm chế biến sẵn", ProductName = "Chả Lụa Ước Lễ Truyền Thống Đặc Sản Đòn 500g", Price = 110000, Address = "Chợ Hôm (Hà Nội)", Rating = 4.9, ReviewCount = 42, ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600" }
            };

            foreach (var item in produceFallbacks)
            {
                if (result.Count >= count) break;
                if (!result.Any(r => r.Id == item.Id || r.ProductName == item.ProductName))
                {
                    result.Add(item);
                }
            }

            return result;
        }


        public async Task<List<ProductCommentDto>> GetCommentsAsync(string productId)
        {
            string key = string.IsNullOrWhiteSpace(productId) ? "1" : productId;

            try
            {
                var dbComments = await _db.ProductComments
                    .AsNoTracking()
                    .Where(c => c.ProductId == key)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return dbComments.Select(c => new ProductCommentDto
                {
                    Id = c.Id,
                    UserFullName = c.UserFullName,
                    UserAvatar = !string.IsNullOrEmpty(c.UserAvatar) ? c.UserAvatar : "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100",
                    CommentText = c.CommentText,
                    ImageUrl = c.ImageUrl,
                    CreatedAt = c.CreatedAt,
                    TimeAgo = FormatTimeAgo(c.CreatedAt)
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductService] Error fetching comments: {ex.Message}");
                return new List<ProductCommentDto>();
            }
        }


        public async Task<ProductCommentDto> AddCommentAsync(string productId, CreateCommentDto dto)
        {
            string key = string.IsNullOrWhiteSpace(productId) ? "1" : productId;
            string userName = string.IsNullOrWhiteSpace(dto.UserFullName) ? "Khách hàng" : dto.UserFullName;

            var commentEntity = new ProductComment
            {
                ProductId = key,
                UserFullName = userName,
                UserAvatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100",
                CommentText = dto.CommentText,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _db.ProductComments.Add(commentEntity);
            await _db.SaveChangesAsync();

            return new ProductCommentDto
            {
                Id = commentEntity.Id,
                UserFullName = commentEntity.UserFullName,
                UserAvatar = commentEntity.UserAvatar,
                CommentText = commentEntity.CommentText,
                ImageUrl = commentEntity.ImageUrl,
                CreatedAt = commentEntity.CreatedAt,
                TimeAgo = "Vừa xong"
            };
        }

        private static string FormatTimeAgo(DateTime createdAt)
        {
            var createdUtc = createdAt.Kind == DateTimeKind.Utc ? createdAt : createdAt.ToUniversalTime();
            var diff = DateTime.UtcNow - createdUtc;

            if (diff.TotalSeconds < 60) return "Vừa xong";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
            if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} ngày trước";
            return createdAt.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
        }



        private static Dictionary<string, string> GetSpecificationsForProduct(string name, string? category)
        {
            string lowerName = name.ToLower();

            if (lowerName.Contains("headphone") || lowerName.Contains("tai nghe"))
            {
                return new Dictionary<string, string>
                {
                    { "Tình trạng", "Đã sử dụng (chưa sửa chữa)" },
                    { "Loại phụ kiện", "Màn hình, Tai nghe" },
                    { "Xuất xứ", "Nhật Bản" },
                    { "Chính sách bảo hành", "Còn bảo hành 6 tháng" }
                };
            }

            return new Dictionary<string, string>
            {
                { "Danh mục", category ?? "Nông sản & Thực phẩm" },
                { "Tình trạng", "Tươi ngon mới về trong ngày" },
                { "Chính sách bảo hành", "Đảm bảo chất lượng 100%" }
            };
        }
    }
}

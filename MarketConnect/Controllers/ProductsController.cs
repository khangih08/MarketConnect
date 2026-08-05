using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Elastic.Clients.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly ElasticsearchClient _elasticClient;
        private readonly IProductCompareService _compareService;
        private readonly IMultiMarketProductService _multiMarketService;
        private readonly ApplicationDbContext _db;

        public ProductsController(
            IProductService service,
            ElasticsearchClient elasticClient,
            IProductCompareService compareService,
            IMultiMarketProductService multiMarketService,
            ApplicationDbContext db)
        {
            _service = service;
            _elasticClient = elasticClient;
            _compareService = compareService;
            _multiMarketService = multiMarketService;
            _db = db;
        }

        [HttpGet("by-market/{marketId:int}")]
        public async Task<IActionResult> GetProductsByMarket(int marketId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _multiMarketService.GetProductsByMarketAsync(marketId, page, pageSize);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{id:int}/markets")]
        public async Task<IActionResult> AssignMarkets(int id, [FromBody] List<int> marketIds)
        {
            await _multiMarketService.AssignProductToMarketsAsync(id, marketIds);
            return Ok(new { message = "Cập nhật chợ hiển thị cho sản phẩm thành công" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] string? category, [FromQuery] int? categoryId, [FromQuery] string? market)
        {
            var cleanItems = new List<SearchProductItemDto>();

            // 1. Thử tìm kiếm bằng Elasticsearch (nếu không có bộ lọc category & market đặc thù)
            if (string.IsNullOrWhiteSpace(category) && !categoryId.HasValue && string.IsNullOrWhiteSpace(market))
            {
                try
                {
                    using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(500)))
                    {
                        var searchResponse = await _elasticClient.SearchAsync<ProductDocument>(s => s
                            .Indices(Elastic.Clients.Elasticsearch.Indices.Index("products"))
                            .Size(50)
                            .Query(query => query
                                .Bool(b => b
                                    .Must(m =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(q))
                                        {
                                            m.Match(match => match
                                                .Field(f => f.ProductName)
                                                .Query(q.Trim())
                                            );
                                        }
                                        else
                                        {
                                            m.MatchAll(_ => { });
                                        }
                                    })
                                )
                            )
                        , cts.Token);

                        if (searchResponse.IsValidResponse && searchResponse.Hits != null && searchResponse.Hits.Any())
                        {
                            cleanItems = searchResponse.Hits.Select(h => {
                                var source = h.Source;
                                string finalImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600"; 

                                if (source != null && !string.IsNullOrEmpty(source.ImageUrl) && !source.ImageUrl.Contains("via.placeholder"))
                                {
                                    finalImageUrl = source.ImageUrl;
                                }

                                return new SearchProductItemDto
                                {
                                    Id = h.Id ?? "1",
                                    GroupKey = source?.GroupKey ?? string.Empty,
                                    ProductName = source?.ProductName ?? string.Empty,
                                    Price = source?.Price ?? 0,
                                    ImageUrl = finalImageUrl, 
                                    SoldCount = source?.SoldCount ?? 0,
                                    Rating = source?.Rating ?? 0,
                                    Brand = source?.Brand ?? "Nông sản & Thực phẩm",
                                    CategoryName = source?.Brand ?? "Nông sản & Thực phẩm",
                                    Address = source?.Province ?? "Hà Nội"
                                };
                            }).ToList();

                            return Ok(cleanItems);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Elasticsearch Notice] Falling back to DB search: {ex.Message}");
                }
            }

            // 2. Truy vấn trực tiếp từ SQLite Database với .AsNoTracking()
            try
            {
                var query = _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductMarkets!)
                        .ThenInclude(pm => pm.Market)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var term = q.Trim().ToLower();
                    query = query.Where(p => (p.Name != null && p.Name.ToLower().Contains(term)) ||
                                             (p.Description != null && p.Description.ToLower().Contains(term)) ||
                                             (p.Category != null && p.Category.Name.ToLower().Contains(term)));
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }
                else if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase) && !category.Equals("Tất cả sản phẩm", StringComparison.OrdinalIgnoreCase))
                {
                    var catTerm = category.Trim().ToLower();
                    query = query.Where(p => p.Category != null && p.Category.Name.ToLower() == catTerm);
                }

                if (!string.IsNullOrWhiteSpace(market) && !market.Equals("Tất cả các chợ", StringComparison.OrdinalIgnoreCase))
                {
                    var mTerm = market.Trim().ToLower();
                    query = query.Where(p => (p.Address != null && p.Address.ToLower().Contains(mTerm)) ||
                                             (p.ProductMarkets != null && p.ProductMarkets.Any(pm => pm.Market != null && pm.Market.Name.ToLower().Contains(mTerm))));
                }

                var dbProducts = await query.OrderByDescending(p => p.CreatedAt).Take(50).ToListAsync();


                if (dbProducts.Any())
                {
                    cleanItems = dbProducts.Select(p => new SearchProductItemDto
                    {
                        Id = p.Id.ToString(),
                        GroupKey = !string.IsNullOrEmpty(p.Name) ? p.Name.ToLower().Replace(" ", "-") : "default-group",
                        ProductName = p.Name ?? "Sản phẩm Nông sản",
                        Price = (double)p.Price,
                        ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) && !p.ImageUrl.Contains("placeholder") 
                            ? p.ImageUrl 
                            : "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600",
                        SoldCount = 520,
                        Rating = 4.9,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category?.Name ?? "Nông sản & Thực phẩm",
                        Brand = p.Category?.Name ?? "Nông sản & Thực phẩm",
                        Address = p.Address ?? "Hà Nội",
                        Condition = p.Condition ?? "Tươi mới về trong ngày"
                    }).ToList();

                    return Ok(cleanItems);
                }

                // 3. Fallback mock nếu DB chưa có bản ghi cho từ khóa / danh mục
                var mockList = new List<SearchProductItemDto>
                {
                    new SearchProductItemDto { Id = "1", GroupKey = "tao-fuji-my-farm-to-door-do-ngot-thanh", ProductName = "Táo Fuji Mỹ Farm To Door Đỏ Ngọt Thanh 1kg", Price = 120000.0, ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600", SoldCount = 804, Rating = 4.9, CategoryName = "Rau củ & Trái cây tươi", Brand = "Rau củ & Trái cây tươi" },
                    new SearchProductItemDto { Id = "2", GroupKey = "cam-sanh-tien-giang", ProductName = "Cam Sành Tiền Giang Mọng Nước Ngọt Thanh 2kg", Price = 45000.0, ImageUrl = "https://images.unsplash.com/photo-1611080626919-7cf5a9dbab5b?w=600", SoldCount = 1200, Rating = 4.8, CategoryName = "Rau củ & Trái cây tươi", Brand = "Rau củ & Trái cây tươi" },
                    new SearchProductItemDto { Id = "3", GroupKey = "rau-cai-thao-da-lat", ProductName = "Rau Cải Thảo Đà Lạt Hữu Cơ Sạch 1kg", Price = 25000.0, ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=600", SoldCount = 530, Rating = 4.7, CategoryName = "Rau củ & Trái cây tươi", Brand = "Rau củ & Trái cây tươi" },
                    new SearchProductItemDto { Id = "4", GroupKey = "thit-than-bo-wagyu", ProductName = "Thịt Thăn Bò Wagyu Úc MB 4-5 - Gói 500g Tiêu Chuẩn", Price = 450000.0, ImageUrl = "https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=600", SoldCount = 804, Rating = 5.0, CategoryName = "Thịt & Gia cầm", Brand = "Thịt & Gia cầm" },
                    new SearchProductItemDto { Id = "5", GroupKey = "ga-ta-tha-vuon", ProductName = "Gà Ta Thả Vườn Nguyên Con Tươi Ngon Cấp Sạch", Price = 185000.0, ImageUrl = "https://images.unsplash.com/photo-1587593810167-a84920ea0781?w=600", SoldCount = 412, Rating = 4.8, CategoryName = "Thịt & Gia cầm", Brand = "Thịt & Gia cầm" }
                };

                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase) && !category.Equals("Tất cả sản phẩm", StringComparison.OrdinalIgnoreCase))
                {
                    var catTerm = category.Trim().ToLower();
                    var matchedCat = mockList.Where(m => m.CategoryName != null && m.CategoryName.ToLower().Contains(catTerm)).ToList();
                    if (matchedCat.Any()) return Ok(matchedCat);
                }

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var term = q.Trim().ToLower();
                    var matched = mockList.Where(m => m.ProductName.ToLower().Contains(term) || m.GroupKey.Contains(term)).ToList();
                    if (matched.Any()) return Ok(matched);
                }

                return Ok(mockList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Database Search Notice] {ex.Message}");
            }

            return Ok(cleanItems);
        }

        [HttpGet("/api/Product/SearchCompare")]
        [HttpGet("SearchCompare")]
        public async Task<IActionResult> SearchCompare([FromQuery] CompareFilterDto filter)
        {
            if (string.IsNullOrWhiteSpace(filter.GroupKey))
            {
                return Ok(new ProductCompareResultDto());
            }

            var result = await _compareService.GetPriceComparisonAsync(filter);
            return Ok(result ?? new ProductCompareResultDto());
        }

        [HttpGet("raw/{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            var created = await _service.CreateAsync(product);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product product)
        {
            var updated = await _service.UpdateAsync(id, product);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var detail = await _service.GetProductDetailAsync(id);
            if (detail != null)
            {
                return Ok(detail);
            }

            return NotFound(new { message = "Không tìm thấy sản phẩm" });
        }

        [HttpGet("{id}/related")]
        public async Task<IActionResult> GetRelated(string id)
        {
            var list = await _service.GetRelatedProductsAsync(id, 5);
            return Ok(list);
        }

        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(string id)
        {
            var comments = await _service.GetCommentsAsync(id);
            return Ok(comments);
        }

        [Authorize]
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> PostComment(string id, [FromBody] CreateCommentDto commentDto)
        {
            if (string.IsNullOrWhiteSpace(commentDto.CommentText))
            {
                return BadRequest(new { message = "Nội dung bình luận không được để trống" });
            }

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            User? dbUser = null;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int uid))
            {
                dbUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid);
            }
            if (dbUser == null && !string.IsNullOrEmpty(userEmail))
            {
                dbUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail || u.Name == userEmail);
            }

            if (dbUser != null && !string.IsNullOrWhiteSpace(dbUser.Name))
            {
                commentDto.UserFullName = dbUser.Name;
            }
            else if (!string.IsNullOrEmpty(userEmail))
            {
                commentDto.UserFullName = userEmail.Split('@')[0];
            }
            else if (string.IsNullOrWhiteSpace(commentDto.UserFullName))
            {
                commentDto.UserFullName = "Người dùng MarketConnect";
            }

            var comment = await _service.AddCommentAsync(id, commentDto);
            return Ok(comment);
        }


    }

    public class SearchProductItemDto
    {
        public string Id { get; set; } = "1";
        public string GroupKey { get; set; } = "";
        public string ProductName { get; set; } = "";
        public double Price { get; set; }
        public string ImageUrl { get; set; } = "";
        public int SoldCount { get; set; }
        public double Rating { get; set; } = 5.0;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Brand { get; set; }
        public string? Address { get; set; }
        public string? Condition { get; set; }
    }
}


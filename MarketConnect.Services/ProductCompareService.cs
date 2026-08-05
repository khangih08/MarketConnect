using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using MarketConnect.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ProductCompareService : IProductCompareService
{
    private readonly ElasticsearchClient _elasticClient;
    private static readonly ConcurrentDictionary<string, List<ProductCommentDto>> _commentsStore = new();

    public ProductCompareService(ElasticsearchClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    public async Task<ProductCompareResultDto?> GetPriceComparisonAsync(CompareFilterDto filter)
    {
        var filterQueries = new List<Action<QueryDescriptor<ProductDocument>>>();

        if (filter.MinPrice.HasValue || filter.MaxPrice.HasValue)
        {
            filterQueries.Add(q => q.Range(r => r
                .Number(nr => nr
                    .Field(p => p.Price)
                    .Gte(filter.MinPrice)
                    .Lte(filter.MaxPrice)
                )
            ));
        }

        if (!string.IsNullOrEmpty(filter.Location) && !filter.Location.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filterQueries.Add(q => q.Term(t => t
                .Field(p => p.Province)
                .Value(filter.Location)
            ));
        }

        var searchResponse = await _elasticClient.SearchAsync<ProductDocument>(s => s
            .Indices("products")
            .Query(q => q.Bool(b => b
                .Must(m => m.Term(t => t.Field(f => f.GroupKey).Value(filter.GroupKey ?? string.Empty)))
                .Filter(filterQueries.ToArray())
            ))
            .Collapse(c => c
                .Field(f => f.GroupKey)
                .InnerHits(ih => ih
                    .Name("other_shops")
                    .Size(30)
                    .Sort(so =>
                    {
                        if (filter.SortBy == "price_desc")
                            so.Field(f => f.Price, g => g.Order(SortOrder.Desc));
                        else if (filter.SortBy == "sold_desc")
                            so.Field(f => f.SoldCount, g => g.Order(SortOrder.Desc));
                        else if (filter.SortBy == "rating_desc")
                            so.Field(f => f.Rating, g => g.Order(SortOrder.Desc));
                        else
                            so.Field(f => f.Price, g => g.Order(SortOrder.Asc));
                    })
                )
            )
        );

        if (!searchResponse.IsValidResponse || searchResponse.Hits == null || !searchResponse.Hits.Any())
            return null;

        var hit = searchResponse.Hits.FirstOrDefault();
        if (hit == null || hit.InnerHits == null || !hit.InnerHits.ContainsKey("other_shops"))
            return null;

        var innerHitsMetaData = hit.InnerHits["other_shops"];

        var innerHitsResult = innerHitsMetaData.Hits.Hits
            .Select(h => h.Source)
            .Where(p => p != null)
            .Cast<ProductDocument>()
            .ToList();

        if (!innerHitsResult.Any())
            return null;

        return new ProductCompareResultDto
        {
            MinPrice = innerHitsResult.Min(p => p.Price),
            MaxPrice = innerHitsResult.Max(p => p.Price),
            Shops = innerHitsResult.Select(p => new ShopProductDetailDto
            {
                ProductId = p.Id ?? string.Empty,
                ShopId = p.ShopId ?? string.Empty,
                ShopName = p.ShopName ?? string.Empty,
                VariantName = p.VariantName ?? string.Empty,
                Price = p.Price,
                Province = p.Province ?? string.Empty,
                SoldCount = p.SoldCount,
                Rating = p.Rating
            }).ToList()
        };
    }

    public async Task<ProductDocument?> GetProductByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        var response = await _elasticClient.GetAsync<ProductDocument>(id, g => g.Index("products"));

        if (!response.IsValidResponse || !response.Found)
            return null;

        return response.Source;
    }

    public async Task<ProductDetailDto?> GetProductDetailAsync(string id, string? groupKey)
    {
        ProductDocument? doc = null;

        if (!string.IsNullOrEmpty(id))
        {
            doc = await GetProductByIdAsync(id);
        }

        if (doc == null && !string.IsNullOrEmpty(groupKey))
        {
            var searchResponse = await _elasticClient.SearchAsync<ProductDocument>(s => s
                .Indices("products")
                .Size(1)
                .Query(q => q.Term(t => t.Field(f => f.GroupKey).Value(groupKey)))
            );
            if (searchResponse.IsValidResponse && searchResponse.Hits != null && searchResponse.Hits.Any())
            {
                doc = searchResponse.Hits.First().Source;
            }
        }

        string prodName = doc?.ProductName ?? "Sản phẩm đăng bán trực tiếp";
        string actualGroupKey = doc?.GroupKey ?? groupKey ?? "default-group";
        double basePrice = doc?.Price ?? 6790000;

        List<string> gallery = doc?.GalleryImages ?? new List<string>();
        if (!gallery.Any())
        {
            string mainImg = doc?.ImageUrl ?? "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=600";
            gallery = new List<string> { mainImg };
        }

        var comments = await GetCommentsAsync(id ?? "default");

        return new ProductDetailDto
        {
            Id = doc?.Id ?? id ?? "1",
            ProductName = prodName,
            GroupKey = actualGroupKey,
            Brand = !string.IsNullOrEmpty(doc?.Brand) ? doc.Brand : "Cá nhân",
            Description = !string.IsNullOrEmpty(doc?.Description) 
                ? doc.Description 
                : $"Sản phẩm <strong>{prodName}</strong> đăng bán trực tiếp trên MarketConnect.",
            ImageUrl = gallery.FirstOrDefault() ?? "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=600",
            GalleryImages = gallery,
            Price = basePrice,
            IsFree = false,
            Address = doc?.Province ?? "Hà Nội",
            SellerType = "Cá nhân",
            Condition = "Đã sử dụng",
            SoldCount = doc?.SoldCount > 0 ? doc.SoldCount : 1,
            Rating = doc?.Rating > 0 ? doc.Rating : 4.9,
            IsBestSeller = doc?.IsBestSeller ?? false,
            DiscountPercent = doc?.DiscountPercent > 0 ? doc.DiscountPercent : 0,
            Specifications = doc?.Specifications ?? GetDefaultSpecifications(prodName),
            SellerInfo = new SellerInfoDto
            {
                SellerId = doc?.ShopId ?? "1",
                SellerName = doc?.ShopName ?? "Người bán MarketConnect",
                SellerAvatar = doc?.ShopLogo ?? "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150",
                SellerType = "Cá nhân",
                Rating = doc?.ShopRating > 0 ? doc.ShopRating : 4.9,
                TotalProducts = doc?.ShopTotalProducts > 0 ? doc.ShopTotalProducts : 5,
                IsOnline = true,
                LastActive = "5 phút trước",
                Phone = doc?.ShopPhone ?? "0988 123 456",
                Address = doc?.Province ?? "Hà Nội"
            },
            Comments = comments
        };
    }

    public async Task<List<RelatedProductDto>> GetRelatedProductsAsync(string id, string? groupKey, int count = 5)
    {
        return new List<RelatedProductDto>
        {
            new RelatedProductDto
            {
                Id = "rel-1",
                GroupKey = "headphones",
                Brand = "Cá nhân",
                ProductName = "Tai nghe Bluetooth Sony WH-1000XM5",
                Price = 6790000,
                Address = "Quận Ba Đình, Hà Nội",
                Rating = 4.9,
                ReviewCount = 12,
                ImageUrl = "/images/seed/headphones.svg"
            },
            new RelatedProductDto
            {
                Id = "rel-2",
                GroupKey = "books",
                Brand = "Bán chuyên",
                ProductName = "Sách C# In Depth",
                Price = 350000,
                Address = "Quận Cầu Giấy, Hà Nội",
                Rating = 4.9,
                ReviewCount = 8,
                ImageUrl = "/images/seed/book.svg"
            }
        };
    }

    public async Task<List<ProductCommentDto>> GetCommentsAsync(string productId)
    {
        string key = productId ?? "default";
        if (!_commentsStore.ContainsKey(key))
        {
            _commentsStore[key] = new List<ProductCommentDto>
            {
                new ProductCommentDto
                {
                    Id = 1,
                    UserFullName = "Nguyễn Văn Hùng",
                    UserAvatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100",
                    CommentText = "Sản phẩm dùng rất mượt, giao hàng đúng hẹn!",
                    CreatedAt = DateTime.Now.AddHours(-3),
                    TimeAgo = "3 giờ trước"
                }
            };
        }

        return _commentsStore[key];
    }

    public async Task<ProductCommentDto> AddCommentAsync(string productId, CreateCommentDto dto)
    {
        string key = productId ?? "default";
        var comments = await GetCommentsAsync(key);

        var newComment = new ProductCommentDto
        {
            Id = comments.Count + 1,
            UserFullName = string.IsNullOrWhiteSpace(dto.UserFullName) ? "Khách hàng" : dto.UserFullName,
            UserAvatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100",
            CommentText = dto.CommentText,
            CreatedAt = DateTime.Now,
            TimeAgo = "Vừa xong"
        };

        comments.Insert(0, newComment);
        return newComment;
    }

    private static Dictionary<string, string> GetDefaultSpecifications(string name)
    {
        return new Dictionary<string, string>
        {
            { "Tình trạng", "Đã sử dụng (chưa sửa chữa)" },
            { "Bảo hành", "Còn bảo hành 6 tháng" }
        };
    }
}

public class ProductDocument
{
    public string? Id { get; set; }
    public string? ProductName { get; set; }
    public string? GroupKey { get; set; }
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? GalleryImages { get; set; }
    public string? ShopId { get; set; }
    public string? ShopName { get; set; }
    public string? ShopLogo { get; set; }
    public string? ShopPhone { get; set; }
    public double ShopRating { get; set; }
    public int ShopTotalProducts { get; set; }
    public string? VariantName { get; set; }
    public double Price { get; set; }
    public int DiscountPercent { get; set; }
    public bool IsBestSeller { get; set; }
    public Dictionary<string, string>? Specifications { get; set; }
    public string? Province { get; set; }
    public int SoldCount { get; set; }
    public double Rating { get; set; }
}

public class CompareFilterDto
{
    public string? GroupKey { get; set; }
    public string? SortBy { get; set; }
    public string? Location { get; set; }
    public double? MinPrice { get; set; }
    public double? MaxPrice { get; set; }
}

public class ProductCompareResultDto
{
    public double MinPrice { get; set; }
    public double MaxPrice { get; set; }
    public List<ShopProductDetailDto> Shops { get; set; } = new List<ShopProductDetailDto>();
}

public class ShopProductDetailDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public double Price { get; set; }
    public string Province { get; set; } = string.Empty;
    public int SoldCount { get; set; }
    public double Rating { get; set; }
}
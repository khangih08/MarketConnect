using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public class MultiMarketProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public bool IsFree { get; set; }
        public string? ImageUrl { get; set; }
        public string? Condition { get; set; }
        public string? Address { get; set; }
        public string? SellerName { get; set; }
        public long CreatedAtUnixMs { get; set; }
    }

    public class MultiMarketPagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalItems { get; set; }
    }

    public interface IMultiMarketProductService
    {
        Task<MultiMarketPagedResult<MultiMarketProductDto>> GetProductsByMarketAsync(int marketId, int page = 1, int pageSize = 20);
        Task AssignProductToMarketsAsync(int productId, List<int> marketIds);
        Task InvalidateProductCacheAsync(int productId, List<int>? affectedMarketIds = null);
    }
}

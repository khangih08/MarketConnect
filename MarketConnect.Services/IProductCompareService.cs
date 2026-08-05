using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarketConnect.Services
{
    public interface IProductCompareService
    {
        Task<ProductCompareResultDto?> GetPriceComparisonAsync(CompareFilterDto filter);
        Task<ProductDocument?> GetProductByIdAsync(string id);
        Task<ProductDetailDto?> GetProductDetailAsync(string id, string? groupKey);
        Task<List<RelatedProductDto>> GetRelatedProductsAsync(string id, string? groupKey, int count = 5);
        Task<ProductCommentDto> AddCommentAsync(string productId, CreateCommentDto dto);
        Task<List<ProductCommentDto>> GetCommentsAsync(string productId);
    }
}

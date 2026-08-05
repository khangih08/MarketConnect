using MarketConnect.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarketConnect.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateAsync(Product product);
        Task<Product> CreateListingAsync(ProductCreateDto dto, int? userId = null);
        Task<Product?> UpdateAsync(int id, Product product);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<ProductDocument?> GetProductByIdAsync(string id);
        Task<ProductDocument?> GetById(string id);
        Task<ProductDetailDto?> GetProductDetailAsync(string id);
        Task<List<RelatedProductDto>> GetRelatedProductsAsync(string id, int count = 5);
        Task<ProductCommentDto> AddCommentAsync(string productId, CreateCommentDto dto);
        Task<List<ProductCommentDto>> GetCommentsAsync(string productId);
    }
}

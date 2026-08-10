using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public class CartGroupDto
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = null!;
        public string VerifiedPhone { get; set; } = null!;
        public string PickupMethods { get; set; } = null!;
        public string? ContactChannelsJson { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public decimal ReferenceTotal { get; set; }
    }

    public interface IMultiMerchantCartService
    {
        Task<CartItem> AddToCartAsync(int buyerId, int productId, int quantity, string? options, string? note);
        Task<List<CartGroupDto>> GetCartGroupedByMerchantAsync(int buyerId);
        Task<bool> RemoveFromCartAsync(int cartItemId, int buyerId);
        Task<List<PurchaseRequest>> CreatePurchaseRequestsFromCartAsync(int buyerId, string buyerName, string buyerPhone, Dictionary<int, string>? storeNotes = null);
        Task<List<PurchaseRequest>> GetPurchaseRequestsForBuyerAsync(int buyerId);
        Task<List<PurchaseRequest>> GetPurchaseRequestsForMerchantStoreAsync(int storeId);
        Task<bool> UpdateRequestStatusAsync(int requestId, PurchaseRequestStatus newStatus);
    }
}

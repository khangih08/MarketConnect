using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public interface IMerchantStoreService
    {
        Task<Store> CreateStoreAsync(int userId, Store storeDto);
        Task<Store?> GetStoreByIdAsync(int storeId);
        Task<List<Store>> GetStoresByUserIdAsync(int userId);
        Task<List<Store>> GetStoresByMarketAsync(int marketId, StoreStatus? status = StoreStatus.Approved);
        Task<List<Store>> GetAllStoresForModerationAsync(StoreStatus? status = null);
        Task<List<Market>> GetAllMarketsAsync();
        Task<Store> UpdateStoreAsync(int storeId, Store updatedStore);
        Task<bool> UpdateStoreStatusAsync(int storeId, StoreStatus newStatus, string? rejectionReason = null);
    }
}

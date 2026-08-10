using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class MerchantStoreService : IMerchantStoreService
    {
        private readonly ApplicationDbContext _db;

        public MerchantStoreService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Store> CreateStoreAsync(int userId, Store store)
        {
            store.UserId = userId;
            store.Status = StoreStatus.PendingApproval;
            store.CreatedAt = DateTime.UtcNow;
            store.UpdatedAt = DateTime.UtcNow;

            _db.Stores.Add(store);
            await _db.SaveChangesAsync();
            return store;
        }

        public async Task<Store?> GetStoreByIdAsync(int storeId)
        {
            return await _db.Stores
                .Include(s => s.Market)
                .Include(s => s.Category)
                .Include(s => s.Owner)
                .Include(s => s.Products)
                .Include(s => s.Reviews)
                .FirstOrDefaultAsync(s => s.Id == storeId);
        }

        public async Task<List<Store>> GetStoresByUserIdAsync(int userId)
        {
            return await _db.Stores
                .Include(s => s.Market)
                .Include(s => s.Category)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Store>> GetStoresByMarketAsync(int marketId, StoreStatus? status = StoreStatus.Approved)
        {
            var query = _db.Stores
                .Include(s => s.Category)
                .Where(s => s.MarketId == marketId);

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            return await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        }

        public async Task<Store> UpdateStoreAsync(int storeId, Store updatedStore)
        {
            var existing = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
            if (existing == null) throw new KeyNotFoundException($"Store {storeId} not found");

            existing.StoreName = updatedStore.StoreName;
            existing.RepresentativeName = updatedStore.RepresentativeName;
            existing.StallLocation = updatedStore.StallLocation;
            existing.CategoryId = updatedStore.CategoryId;
            existing.Description = updatedStore.Description;
            existing.LogoUrl = updatedStore.LogoUrl;
            existing.CoverUrl = updatedStore.CoverUrl;
            existing.PhotoProofUrl = updatedStore.PhotoProofUrl;
            existing.ContactChannelsJson = updatedStore.ContactChannelsJson;
            existing.PickupMethods = updatedStore.PickupMethods;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<List<Market>> GetAllMarketsAsync()
        {
            return await _db.Markets.OrderBy(m => m.Name).ToListAsync();
        }

        public async Task<List<Store>> GetAllStoresForModerationAsync(StoreStatus? status = null)
        {
            var query = _db.Stores
                .Include(s => s.Market)
                .Include(s => s.Category)
                .Include(s => s.Owner)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            return await query.OrderByDescending(s => s.UpdatedAt).ToListAsync();
        }

        public async Task<bool> UpdateStoreStatusAsync(int storeId, StoreStatus newStatus, string? rejectionReason = null)
        {
            var existing = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
            if (existing == null) return false;

            // Bắt buộc nhập lý do khi Từ chối, Tạm ngừng hoặc Khóa hồ sơ
            if (newStatus == StoreStatus.Rejected || newStatus == StoreStatus.Suspended || newStatus == StoreStatus.Locked)
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    throw new InvalidOperationException("Quản trị viên phải nhập lý do khi từ chối, tạm ngừng hoặc khóa hồ sơ gian hàng.");
                }
            }

            existing.Status = newStatus;
            existing.RejectionReason = rejectionReason;
            existing.UpdatedAt = DateTime.UtcNow;

            // Khi duyệt hồ sơ -> Nâng quyền tài khoản chủ sở hữu gian hàng thành Tiểu thương (Merchant)
            if (newStatus == StoreStatus.Approved)
            {
                var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId);
                if (owner != null && owner.Role == UserRole.Buyer)
                {
                    owner.Role = UserRole.Merchant;
                    _db.Users.Update(owner);
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }
}

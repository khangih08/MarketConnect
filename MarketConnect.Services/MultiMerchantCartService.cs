using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class MultiMerchantCartService : IMultiMerchantCartService
    {
        private readonly ApplicationDbContext _db;

        public MultiMerchantCartService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CartItem> AddToCartAsync(int buyerId, int productId, int quantity, string? options, string? note)
        {
            var product = await _db.Products.Include(p => p.Store).FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) throw new KeyNotFoundException($"Product {productId} not found");

            int storeId = product.StoreId ?? 0;
            if (storeId == 0)
            {
                // Tìm hoặc tạo Store mặc định nếu sản phẩm chưa được gán StoreId
                var firstStore = await _db.Stores.FirstOrDefaultAsync();
                if (firstStore != null) storeId = firstStore.Id;
            }

            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(c => c.BuyerId == buyerId && c.ProductId == productId && c.SelectedOptions == options);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.Note = note ?? existingItem.Note;
                await _db.SaveChangesAsync();
                return existingItem;
            }

            var cartItem = new CartItem
            {
                BuyerId = buyerId,
                ProductId = productId,
                StoreId = storeId,
                Quantity = quantity,
                SelectedOptions = options,
                Note = note,
                CreatedAt = DateTime.UtcNow
            };

            _db.CartItems.Add(cartItem);
            await _db.SaveChangesAsync();
            return cartItem;
        }

        public async Task<List<CartGroupDto>> GetCartGroupedByMerchantAsync(int buyerId)
        {
            var items = await _db.CartItems
                .Include(c => c.Product)
                .Include(c => c.Store)
                .Where(c => c.BuyerId == buyerId)
                .ToListAsync();

            var groups = items.GroupBy(i => i.StoreId)
                .Select(g => {
                    var firstItem = g.First();
                    var store = firstItem.Store;
                    var storeName = store?.StoreName ?? "Gian Hàng Tiểu Thương";
                    var phone = store?.VerifiedPhone ?? "Liên hệ tiểu thương";
                    var pickup = store?.PickupMethods ?? "Nhận tại quầy / Tự thỏa thuận";

                    decimal refTotal = g.Where(i => i.Product != null && i.Product.PriceType != "Contact")
                                         .Sum(i => (i.Product?.Price ?? 0) * i.Quantity);

                    return new CartGroupDto
                    {
                        StoreId = g.Key,
                        StoreName = storeName,
                        VerifiedPhone = phone,
                        PickupMethods = pickup,
                        ContactChannelsJson = store?.ContactChannelsJson,
                        Items = g.ToList(),
                        ReferenceTotal = refTotal
                    };
                }).ToList();

            return groups;
        }

        public async Task<bool> RemoveFromCartAsync(int cartItemId, int buyerId)
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.BuyerId == buyerId);
            if (item == null) return false;

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<PurchaseRequest>> CreatePurchaseRequestsFromCartAsync(int buyerId, string buyerName, string buyerPhone, Dictionary<int, string>? storeNotes = null, string? preferredPickupMethod = null)
        {
            var cartGroups = await GetCartGroupedByMerchantAsync(buyerId);
            if (!cartGroups.Any()) return new List<PurchaseRequest>();

            var requests = new List<PurchaseRequest>();
            var timestampStr = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            foreach (var group in cartGroups)
            {
                string noteForStore = storeNotes != null && storeNotes.ContainsKey(group.StoreId) ? storeNotes[group.StoreId] : "";

                var req = new PurchaseRequest
                {
                    RequestCode = $"PR-{timestampStr}-{group.StoreId}-{Random.Shared.Next(100, 999)}",
                    BuyerId = buyerId,
                    StoreId = group.StoreId,
                    Status = PurchaseRequestStatus.Sent,
                    BuyerName = buyerName,
                    BuyerPhone = buyerPhone,
                    PreferredPickupMethod = !string.IsNullOrWhiteSpace(preferredPickupMethod) ? preferredPickupMethod : group.PickupMethods,
                    Note = noteForStore,
                    ReferenceTotalPrice = group.ReferenceTotal,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Items = group.Items.Select(i => new PurchaseRequestItem
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product?.Name ?? "Sản phẩm tiểu thương",
                        Price = i.Product?.Price ?? 0,
                        Quantity = i.Quantity,
                        OptionsNote = i.SelectedOptions
                    }).ToList()
                };

                requests.Add(req);
            }

            _db.PurchaseRequests.AddRange(requests);

            // Xóa các sản phẩm đã gửi trong giỏ
            var cartItemsToRemove = cartGroups.SelectMany(g => g.Items).ToList();
            _db.CartItems.RemoveRange(cartItemsToRemove);

            await _db.SaveChangesAsync();
            return requests;
        }

        public async Task<List<PurchaseRequest>> GetPurchaseRequestsForBuyerAsync(int buyerId)
        {
            return await _db.PurchaseRequests
                .Include(r => r.Store)
                .Include(r => r.Items)
                .Where(r => r.BuyerId == buyerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PurchaseRequest>> GetPurchaseRequestsForMerchantStoreAsync(int storeId)
        {
            return await _db.PurchaseRequests
                .Include(r => r.Buyer)
                .Include(r => r.Items)!
                    .ThenInclude(i => i.Product)
                .Where(r => r.StoreId == storeId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateRequestStatusAsync(int requestId, PurchaseRequestStatus newStatus)
        {
            var req = await _db.PurchaseRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return false;

            req.Status = newStatus;
            req.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}

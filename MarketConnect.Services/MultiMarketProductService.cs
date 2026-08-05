using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace MarketConnect.Services
{
    public class MultiMarketProductService : IMultiMarketProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConnectionMultiplexer? _redis;
        private readonly IDatabase? _cache;

        public MultiMarketProductService(ApplicationDbContext db, IConnectionMultiplexer? redis = null)
        {
            _db = db;
            _redis = redis;
            if (_redis != null && _redis.IsConnected)
            {
                _cache = _redis.GetDatabase();
            }
        }

        public async Task<MultiMarketPagedResult<MultiMarketProductDto>> GetProductsByMarketAsync(int marketId, int page = 1, int pageSize = 20)
        {
            int start = (page - 1) * pageSize;
            int stop = start + pageSize - 1;

            // 1. Thử đọc từ Redis Cache (ZSET & Hash)
            if (_cache != null)
            {
                try
                {
                    string zsetKey = $"market:{marketId}:products";
                    RedisValue[] productIdsRedis = await _cache.SortedSetRangeByRankAsync(zsetKey, start, stop, Order.Descending);
                    long totalItems = await _cache.SortedSetLengthAsync(zsetKey);

                    if (productIdsRedis.Length > 0)
                    {
                        var cachedDetails = await GetProductsDetailsFromCacheAsync(productIdsRedis);
                        if (cachedDetails.Count == productIdsRedis.Length)
                        {
                            return new MultiMarketPagedResult<MultiMarketProductDto>
                            {
                                Items = cachedDetails,
                                Page = page,
                                PageSize = pageSize,
                                TotalItems = totalItems
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log Redis error and fallback gracefully to Database
                    Console.WriteLine($"[Redis Cache Error] {ex.Message}");
                }
            }

            // 2. Cache Miss / Partial Miss / Redis Offline -> Query Database sử dụng Composite Index
            return await PopulateCacheFromDatabaseAsync(marketId, page, pageSize, start, stop);
        }

        private async Task<List<MultiMarketProductDto>> GetProductsDetailsFromCacheAsync(RedisValue[] productIds)
        {
            if (_cache == null) return new List<MultiMarketProductDto>();

            var keys = Array.ConvertAll(productIds, id => (RedisKey)$"product:{id}");
            var values = await _cache.StringGetAsync(keys);

            var result = new List<MultiMarketProductDto>();
            foreach (var val in values)
            {
                if (val.HasValue)
                {
                    var dto = JsonSerializer.Deserialize<MultiMarketProductDto>(val.ToString()!);
                    if (dto != null) result.Add(dto);
                }
            }
            return result;
        }

        private async Task<MultiMarketPagedResult<MultiMarketProductDto>> PopulateCacheFromDatabaseAsync(int marketId, int page, int pageSize, int start, int stop)
        {
            var query = _db.ProductMarkets
                .AsNoTracking()
                .Where(pm => pm.MarketId == marketId)
                .OrderByDescending(pm => pm.CreatedAt)
                .Select(pm => new
                {
                    pm.ProductId,
                    pm.CreatedAt,
                    pm.Product,
                    SellerName = pm.Product != null && pm.Product.Seller != null ? pm.Product.Seller.Name : "Tiểu thương Chợ"
                });

            long totalItems = await _db.ProductMarkets.CountAsync(pm => pm.MarketId == marketId);
            var dbRecords = await query.Skip(start).Take(pageSize).ToListAsync();

            var dtos = new List<MultiMarketProductDto>();

            IBatch? batch = _cache?.CreateBatch();
            string zsetKey = $"market:{marketId}:products";

            foreach (var item in dbRecords)
            {
                if (item.Product == null) continue;

                var dto = new MultiMarketProductDto
                {
                    Id = item.ProductId,
                    Name = item.Product.Name,
                    Price = item.Product.Price,
                    IsFree = item.Product.IsFree,
                    ImageUrl = item.Product.ImageUrl,
                    Condition = item.Product.Condition,
                    Address = item.Product.Address,
                    SellerName = item.SellerName,
                    CreatedAtUnixMs = new DateTimeOffset(item.CreatedAt).ToUnixTimeMilliseconds()
                };
                dtos.Add(dto);

                if (batch != null)
                {
                    // Populate ZSET Index
                    _ = batch.SortedSetAddAsync(zsetKey, item.ProductId.ToString(), dto.CreatedAtUnixMs);

                    // Populate Detail Entity (TTL 24h)
                    string detailKey = $"product:{item.ProductId}";
                    string json = JsonSerializer.Serialize(dto);
                    _ = batch.StringSetAsync(detailKey, json, TimeSpan.FromHours(24));

                    // Track market mapping
                    string marketSetKey = $"product:{item.ProductId}:markets";
                    _ = batch.SetAddAsync(marketSetKey, marketId.ToString());
                }
            }

            batch?.Execute();

            return new MultiMarketPagedResult<MultiMarketProductDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        public async Task AssignProductToMarketsAsync(int productId, List<int> marketIds)
        {
            var existing = await _db.ProductMarkets.Where(pm => pm.ProductId == productId).ToListAsync();
            _db.ProductMarkets.RemoveRange(existing);

            var now = DateTime.UtcNow;
            var nowUnixMs = new DateTimeOffset(now).ToUnixTimeMilliseconds();

            foreach (var mId in marketIds)
            {
                _db.ProductMarkets.Add(new ProductMarket
                {
                    MarketId = mId,
                    ProductId = productId,
                    CreatedAt = now
                });
            }

            await _db.SaveChangesAsync();

            // Invalidate old cache
            await InvalidateProductCacheAsync(productId, existing.Select(e => e.MarketId).ToList());

            if (_cache != null)
            {
                var batch = _cache.CreateBatch();
                string marketSetKey = $"product:{productId}:markets";

                foreach (var mId in marketIds)
                {
                    _ = batch.SortedSetAddAsync($"market:{mId}:products", productId.ToString(), nowUnixMs);
                    _ = batch.SetAddAsync(marketSetKey, mId.ToString());
                }

                batch.Execute();
            }
        }

        public async Task InvalidateProductCacheAsync(int productId, List<int>? affectedMarketIds = null)
        {
            if (_cache == null) return;

            try
            {
                var batch = _cache.CreateBatch();

                // 1. Delete Product Entity Detail Cache
                _ = batch.KeyDeleteAsync($"product:{productId}");

                // 2. Resolve Market IDs
                List<int> targetMarkets = affectedMarketIds ?? new List<int>();

                if (targetMarkets.Count == 0)
                {
                    var redisMarkets = await _cache.SetMembersAsync($"product:{productId}:markets");
                    targetMarkets = redisMarkets.Select(m => (int)m!).ToList();
                }

                // 3. Remove product ID from all affected market ZSETs
                foreach (var mId in targetMarkets)
                {
                    _ = batch.SortedSetRemoveAsync($"market:{mId}:products", productId.ToString());
                }

                // 4. Delete tracking markets set
                _ = batch.KeyDeleteAsync($"product:{productId}:markets");

                batch.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Redis Invalidation Error] {ex.Message}");
            }
        }
    }
}

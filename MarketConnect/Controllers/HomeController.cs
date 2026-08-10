using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MarketConnect.Data;
using MarketConnect.Models;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _products;
        private readonly ApplicationDbContext _db;
        private readonly IMerchantStoreService _storeService;
        private readonly IMultiMerchantCartService _cartService;

        public HomeController(
            IProductService products,
            ApplicationDbContext db,
            IMerchantStoreService storeService,
            IMultiMerchantCartService cartService)
        {
            _products = products;
            _db = db;
            _storeService = storeService;
            _cartService = cartService;
        }

        public async Task<IActionResult> Index(string? q, int? marketId)
        {
            // Lấy danh sách sản phẩm
            var items = await _products.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                items = items.Where(p => p.Name != null && p.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            // Lấy danh sách chợ kèm phường/quận
            var markets = await _db.Markets
                .Include(m => m.Ward)
                .Include(m => m.District)
                .Include(m => m.Province)
                .Where(m => m.IsActive)
                .ToListAsync();
            ViewBag.Markets = markets;

            if (!marketId.HasValue && Request.Cookies.TryGetValue("SelectedMarketId", out var cookieMkt) && int.TryParse(cookieMkt, out int cookieMktId))
            {
                marketId = cookieMktId;
            }

            // Chợ được chọn
            Market? selectedMarket = null;
            if (marketId.HasValue)
            {
                selectedMarket = markets.FirstOrDefault(m => m.Id == marketId.Value);
            }
            if (selectedMarket == null)
            {
                selectedMarket = markets.FirstOrDefault(m => m.Slug == "cho-nhan-chinh") ?? markets.FirstOrDefault();
            }
            ViewBag.SelectedMarket = selectedMarket;

            // Lấy danh sách gian hàng thuộc CHÍNH XÁC chợ đang chọn
            var stores = await _db.Stores
                .Include(s => s.Category)
                .Include(s => s.Market)
                .Include(s => s.Reviews)
                .Where(s => s.Status == StoreStatus.Approved && selectedMarket != null && s.MarketId == selectedMarket.Id)
                .ToListAsync();

            ViewBag.Stores = stores;

            // Lọc sản phẩm thuộc CHÍNH XÁC chợ đang chọn
            if (selectedMarket != null)
            {
                var storeIdsInMarket = stores.Select(s => s.Id).ToList();
                var productIdsInMarket = new List<int>();
                try
                {
                    productIdsInMarket = await _db.ProductMarkets
                        .Where(pm => pm.MarketId == selectedMarket.Id)
                        .Select(pm => pm.ProductId)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProductMarkets Query Notice] {ex.Message}");
                }

                items = items.Where(p => (productIdsInMarket.Count > 0 && productIdsInMarket.Contains(p.Id)) || (p.StoreId.HasValue && storeIdsInMarket.Contains(p.StoreId.Value)));
            }

            // Lấy số lượng giỏ hàng của user
            int buyerId = 1;
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(subClaim, out int parsedId)) buyerId = parsedId;

            var cartGroups = await _cartService.GetCartGroupedByMerchantAsync(buyerId);
            int cartCount = cartGroups.Sum(g => g.Items.Count);
            ViewBag.CartItemCount = cartCount;

            // Lấy danh mục
            ViewBag.Categories = await _db.Categories.ToListAsync();

            return View(items);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

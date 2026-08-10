using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketConnect.Controllers
{
    public class CartAndOrdersController : Controller
    {
        private readonly IMultiMerchantCartService _cartService;
        private readonly ApplicationDbContext _db;

        public CartAndOrdersController(IMultiMerchantCartService cartService, ApplicationDbContext db)
        {
            _cartService = cartService;
            _db = db;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(subClaim, out int parsedId))
                {
                    var u = await _db.Users.FindAsync(parsedId);
                    if (u != null) return u;
                }

                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity.Name;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var u = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Users, x => x.Email == userEmail);
                    if (u != null) return u;
                }
            }

            if (Request.Cookies.TryGetValue("user_id", out var cookieUserId) && int.TryParse(cookieUserId, out int parsedCookieId))
            {
                var u = await _db.Users.FindAsync(parsedCookieId);
                if (u != null) return u;
            }

            if (Request.Cookies.TryGetValue("user_email", out var cookieEmail) && !string.IsNullOrEmpty(cookieEmail))
            {
                var u = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Users, x => x.Email == cookieEmail);
                if (u != null) return u;
            }

            if (Request.Cookies.TryGetValue("user_phone", out var cookiePhone) && !string.IsNullOrEmpty(cookiePhone))
            {
                var u = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Users, x => x.Phone == cookiePhone);
                if (u != null) return u;
            }

            return null;
        }

        private async Task<int> GetCurrentUserIdAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.Id ?? 1;
        }

        // GET: /CartAndOrders
        public async Task<IActionResult> Index()
        {
            int buyerId = await GetCurrentUserIdAsync();
            var cartGroups = await _cartService.GetCartGroupedByMerchantAsync(buyerId);
            return View(cartGroups);
        }

        // POST: /CartAndOrders/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string? options = null, string? note = null)
        {
            int buyerId = await GetCurrentUserIdAsync();
            await _cartService.AddToCartAsync(buyerId, productId, quantity, options, note);
            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        // POST: /CartAndOrders/RemoveItem
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            int buyerId = await GetCurrentUserIdAsync();
            await _cartService.RemoveFromCartAsync(cartItemId, buyerId);
            return RedirectToAction("Index");
        }

        // POST: /CartAndOrders/SubmitPurchaseRequests
        [HttpPost]
        public async Task<IActionResult> SubmitPurchaseRequests(string buyerName, string buyerPhone)
        {
            int buyerId = await GetCurrentUserIdAsync();
            var requests = await _cartService.CreatePurchaseRequestsFromCartAsync(buyerId, buyerName, buyerPhone);
            TempData["SuccessMessage"] = $"Đã gửi thành công {requests.Count} yêu cầu đặt mua tới các tiểu thương tương ứng!";
            return RedirectToAction("BuyerRequests");
        }

        // GET: /CartAndOrders/BuyerRequests
        public async Task<IActionResult> BuyerRequests()
        {
            int buyerId = await GetCurrentUserIdAsync();
            var requests = await _cartService.GetPurchaseRequestsForBuyerAsync(buyerId);
            return View(requests);
        }

        // GET: /CartAndOrders/MerchantRequests
        public async Task<IActionResult> MerchantRequests(int? storeId = null)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập tài khoản tiểu thương.";
                return RedirectToAction("Login", "Account");
            }

            var myStore = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Stores, s => s.UserId == currentUser.Id);
            int targetStoreId = storeId ?? (myStore != null ? myStore.Id : 1);

            var requests = await _cartService.GetPurchaseRequestsForMerchantStoreAsync(targetStoreId);
            ViewBag.StoreId = targetStoreId;
            ViewBag.MyStore = myStore;
            ViewBag.UserRole = currentUser.Role;
            return View(requests);
        }

        // POST: /CartAndOrders/UpdateRequestStatus
        [HttpPost]
        public async Task<IActionResult> UpdateRequestStatus(int requestId, PurchaseRequestStatus status)
        {
            await _cartService.UpdateRequestStatusAsync(requestId, status);
            return RedirectToAction("MerchantRequests");
        }
    }
}

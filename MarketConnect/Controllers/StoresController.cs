using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketConnect.Controllers
{
    public class StoresController : Controller
    {
        private readonly IMerchantStoreService _storeService;
        private readonly ApplicationDbContext _db;

        public StoresController(IMerchantStoreService storeService, ApplicationDbContext db)
        {
            _storeService = storeService;
            _db = db;
        }

        // GET: /Stores
        public async Task<IActionResult> Index(int? marketId)
        {
            if (!marketId.HasValue && Request.Cookies.TryGetValue("SelectedMarketId", out var cookieMkt) && int.TryParse(cookieMkt, out int cookieMktId))
            {
                marketId = cookieMktId;
            }

            if (marketId.HasValue)
            {
                var stores = await _storeService.GetStoresByMarketAsync(marketId.Value);
                ViewBag.MarketId = marketId;
                var activeMarket = await _db.Markets.FindAsync(marketId.Value);
                ViewBag.MarketName = activeMarket?.Name ?? "Chợ được chọn";
                return View(stores);
            }

            var defaultStores = await _storeService.GetStoresByMarketAsync(1);
            ViewBag.MarketName = "Chợ Nhân Chính";
            return View(defaultStores);
        }

        // GET: /Stores/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var store = await _storeService.GetStoreByIdAsync(id);
            if (store == null) return NotFound("Không tìm thấy gian hàng.");

            User? currentUser = await GetCurrentUserAsync();

            // Nếu người dùng này là chủ sở hữu gian hàng APPROVED nhưng role vẫn là Buyer -> Tự động cập nhật thành Merchant
            if (currentUser != null && store.Status == StoreStatus.Approved && store.UserId == currentUser.Id)
            {
                if (currentUser.Role == UserRole.Buyer)
                {
                    currentUser.Role = UserRole.Merchant;
                    _db.Users.Update(currentUser);
                    await _db.SaveChangesAsync();
                }
            }

            // ĐÁNH GIÁ PHÂN QUYỀN ĐĂNG SẢN PHẨM:
            // Chỉ tài khoản TIỂU THƯƠNG ĐÃ ĐƯỢC DUYỆT (UserRole.Merchant) VÀ là CHỦ SỞ HỮU CHÍNH XÁC (store.UserId == currentUser.Id) của gian hàng APPROVED mới có quyền đăng sản phẩm.
            bool canPostProduct = false;

            if (currentUser != null && store.Status == StoreStatus.Approved)
            {
                bool isVerifiedMerchantRole = currentUser.Role == UserRole.Merchant ||
                                              currentUser.Role == UserRole.MobileSeller ||
                                              currentUser.Role == UserRole.MarketAdmin ||
                                              currentUser.Role == UserRole.SuperAdmin;

                bool isExactStoreOwner = store.UserId == currentUser.Id;

                if (isVerifiedMerchantRole && isExactStoreOwner)
                {
                    canPostProduct = true;
                }
            }

            ViewBag.CanPostProduct = canPostProduct;
            ViewBag.Categories = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(_db.Categories.OrderBy(c => c.Name));

            return View(store);
        }

        // POST: /Stores/AddProduct
        [HttpPost]
        public async Task<IActionResult> AddProduct([FromForm] StoreAddProductDto dto, Microsoft.AspNetCore.Http.IFormFile? ImageFile)
        {
            var store = await _db.Stores.FindAsync(dto.StoreId);
            if (store == null) return NotFound("Không tìm thấy gian hàng.");

            User? currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập tài khoản tiểu thương để thực hiện đăng sản phẩm.";
                return RedirectToAction("Detail", new { id = dto.StoreId });
            }

            bool isOwner = store.UserId == currentUser.Id;
            if (isOwner && store.Status == StoreStatus.Approved && currentUser.Role == UserRole.Buyer)
            {
                currentUser.Role = UserRole.Merchant;
                _db.Users.Update(currentUser);
                await _db.SaveChangesAsync();
            }

            bool isMerchantRole = currentUser.Role == UserRole.Merchant || 
                                 currentUser.Role == UserRole.MobileSeller || 
                                 currentUser.Role == UserRole.MarketAdmin || 
                                 currentUser.Role == UserRole.SuperAdmin;

            if (!isOwner || !isMerchantRole || store.Status != StoreStatus.Approved)
            {
                TempData["ErrorMessage"] = "Chỉ tiểu thương sở hữu gian hàng đã phê duyệt mới có quyền đăng sản phẩm!";
                return RedirectToAction("Detail", new { id = dto.StoreId });
            }

            string imageUrl = dto.ImageUrl ?? "/images/seed/headphones.svg";

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!System.IO.Directory.Exists(uploadsFolder)) System.IO.Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString("N") + System.IO.Path.GetExtension(ImageFile.FileName);
                var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                imageUrl = "/uploads/" + uniqueFileName;
            }

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.IsFree ? 0 : dto.Price,
                IsFree = dto.IsFree,
                Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "Cái" : dto.Unit,
                CategoryId = dto.CategoryId > 0 ? dto.CategoryId : 1,
                Condition = string.IsNullOrWhiteSpace(dto.Condition) ? "Tươi mới về trong ngày" : dto.Condition,
                StockStatus = string.IsNullOrWhiteSpace(dto.StockStatus) ? "InStock" : dto.StockStatus,
                ImageUrl = imageUrl,
                StoreId = dto.StoreId,
                UserId = currentUser.Id,
                SellerType = "Bán chuyên",
                CreatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            if (store.MarketId > 0)
            {
                var existsMarketLink = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(_db.ProductMarkets, pm => pm.MarketId == store.MarketId && pm.ProductId == product.Id);
                if (!existsMarketLink)
                {
                    _db.ProductMarkets.Add(new ProductMarket
                    {
                        MarketId = store.MarketId,
                        ProductId = product.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = $"Đã đăng sản phẩm \"{product.Name}\" thành công lên gian hàng!";
            return RedirectToAction("Detail", new { id = dto.StoreId });
        }

        // GET: /Stores/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Markets = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(_db.Markets.OrderBy(m => m.Name));
            ViewBag.Categories = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(_db.Categories.OrderBy(c => c.Name));
            return View();
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

        // POST: /Stores/Create
        [HttpPost]
        public async Task<IActionResult> Create(Store store)
        {
            User? currentUser = await GetCurrentUserAsync();

            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập tài khoản trước khi gửi hồ sơ đăng ký gian hàng.";
                return RedirectToAction("Login", "Account");
            }

            int userId = currentUser.Id;

            if (!ModelState.IsValid)
            {
                ViewBag.Markets = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(_db.Markets.OrderBy(m => m.Name));
                ViewBag.Categories = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(_db.Categories.OrderBy(c => c.Name));
                return View(store);
            }

            store.Status = StoreStatus.Approved;
            var created = await _storeService.CreateStoreAsync(userId, store);

            if (currentUser.Role == UserRole.Buyer)
            {
                currentUser.Role = UserRole.Merchant;
                _db.Users.Update(currentUser);
                await _db.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Chúc mừng! Gian hàng của bạn đã được đăng ký và xác thực vai trò Tiểu thương thành công!";
            return RedirectToAction("Detail", new { id = created.Id });
        }
    }

    public class StoreAddProductDto
    {
        public int StoreId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsFree { get; set; }
        public string Unit { get; set; } = "Cái";
        public int CategoryId { get; set; }
        public string Condition { get; set; } = "Tươi mới về trong ngày";
        public string StockStatus { get; set; } = "InStock";
        public string? ImageUrl { get; set; }
    }
}

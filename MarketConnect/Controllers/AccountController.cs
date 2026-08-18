using MarketConnect.Controllers.Dtos;
using MarketConnect.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("Account/AddPhoneAndSetPassword")]
        public IActionResult AddPhoneAndSetPassword()
        {
            return View("AddPhoneAndSetPassword");
        }

        [Authorize]
        [HttpPost("Account/AddPhoneAndSetPassword")]
        public async Task<IActionResult> AddPhoneAndSetPasswordPost([FromForm] AddPhoneModel model)
        {
            try
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail)) return Unauthorized(new { message = "Chưa đăng nhập" });

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

                if (!string.IsNullOrWhiteSpace(user.Phone))
                {
                    return BadRequest(new { message = "Tài khoản đã có số điện thoại." });
                }

                if (string.IsNullOrWhiteSpace(model.Phone) || string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    return BadRequest(new { message = "Vui lòng cung cấp số điện thoại và mật khẩu mới." });
                }

                // assign phone and hash password
                user.Phone = model.Phone.Trim();
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Đã thêm số điện thoại và thiết lập mật khẩu." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public class AddPhoneModel
        {
            public string? Phone { get; set; }
            public string? NewPassword { get; set; }
        }

        [HttpGet("Account/Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("Account/Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet("Account/Logout")]
        [HttpPost("Account/Logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(HttpContext);
            }
            catch { }

            Response.Cookies.Delete("user_email");
            Response.Cookies.Delete("user_name");
            Response.Cookies.Delete("user_phone");
            Response.Cookies.Delete("MarketConnectAuthCookie");
            Response.Cookies.Delete(".AspNetCore.Cookies");
            Response.Cookies.Delete(".AspNetCore.Identity.Application");

            return Content("<script>sessionStorage.clear(); localStorage.clear(); window.location.href='/Account/Login';</script>", "text/html");
        }

        [HttpGet("Account/Profile")]
        public async Task<IActionResult> Profile()
        {
            User? user = null;

            // 1. Tìm theo Claim NameIdentifier / Email / Name
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(subClaim, out int parsedId))
                {
                    user = await _db.Users.FindAsync(parsedId);
                }

                if (user == null)
                {
                    var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity.Name;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Users, u => u.Email == userEmail || u.Name == userEmail || u.Phone == userEmail);
                    }
                }
            }

            // 2. Fallback tìm theo Cookie nếu Identity chưa đính kèm
            if (user == null)
            {
                if (Request.Cookies.TryGetValue("user_email", out var cookieEmail) && !string.IsNullOrEmpty(cookieEmail))
                {
                    user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Users, u => u.Email == cookieEmail || u.Name == cookieEmail || u.Phone == cookieEmail);
                }
                else if (Request.Cookies.TryGetValue("user_phone", out var cookiePhone) && !string.IsNullOrEmpty(cookiePhone))
                {
                    user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Users, u => u.Phone == cookiePhone || u.Email == cookiePhone);
                }
                else if (Request.Cookies.TryGetValue("user_name", out var cookieName) && !string.IsNullOrEmpty(cookieName))
                {
                    user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Users, u => u.Name == cookieName || u.Email == cookieName);
                }
            }

            // Nếu chưa đọc được user từ Server Cookies -> Tự động đồng bộ từ LocalStorage/SessionStorage phía Client
            if (user == null)
            {
                return Content(@"<script>
                    const email = localStorage.getItem('user_email') || sessionStorage.getItem('user_email');
                    const name = localStorage.getItem('user_name') || sessionStorage.getItem('user_name');
                    const phone = localStorage.getItem('user_phone') || sessionStorage.getItem('user_phone');
                    if (email || name || phone) {
                        if (email) document.cookie = 'user_email=' + encodeURIComponent(email) + '; path=/; max-age=2592000; SameSite=Lax';
                        if (name) document.cookie = 'user_name=' + encodeURIComponent(name) + '; path=/; max-age=2592000; SameSite=Lax';
                        if (phone) document.cookie = 'user_phone=' + encodeURIComponent(phone) + '; path=/; max-age=2592000; SameSite=Lax';
                        window.location.reload();
                    } else {
                        window.location.href = '/Account/Login';
                    }
                </script>", "text/html");
            }

            // Lấy tất cả gian hàng sở hữu CHÍNH XÁC bởi tài khoản người dùng này
            var userStores = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(
                    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(
                        _db.Stores, s => s.Market), s => s.Category)
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.CreatedAt));

            // Cập nhật phân quyền Role: Chuyển sang Merchant nếu người dùng này có gian hàng
            bool hasStore = userStores.Any();
            if (hasStore && user.Role == UserRole.Buyer)
            {
                user.Role = UserRole.Merchant;
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
            }

            ViewBag.UserRole = user.Role;
            ViewBag.UserStores = userStores;

            return View();
        }

        [HttpGet("Account/ChangePassword")]
        public IActionResult ChangePassword()
        {
            return View("ChangePassword");
        }

        [HttpPost("Account/UpdateProfile")] 
        public async Task<IActionResult> UpdateProfile([FromForm] ProfileUpdateModel model)
        {
            User? user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { message = "Chưa đăng nhập hoặc phiên làm việc hết hạn." });
            }

            if (!string.IsNullOrWhiteSpace(model.FullName)) user.Name = model.FullName;
            if (!string.IsNullOrWhiteSpace(model.Phone)) user.Phone = model.Phone;
            user.Address = model.Address;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật hồ sơ thành công!",
                fullName = user.Name,
                phone = user.Phone,
                address = user.Address,
                gender = user.Gender,
                dateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd")
            });
        }

        [HttpGet("Account/GetProfileData")]
        public async Task<IActionResult> GetProfileData()
        {
            User? user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { message = "Chưa đăng nhập hoặc phiên làm việc hết hạn." });
            }

            var userStores = await _db.Stores
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new { s.Id, s.StoreName, status = s.Status.ToString() })
                .ToListAsync();

            bool isMerchant = user.Role == UserRole.Merchant || userStores.Any();
            if (userStores.Any() && user.Role == UserRole.Buyer)
            {
                user.Role = UserRole.Merchant;
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
            }

            return Ok(new
            {
                fullName = user.Name,
                phone = user.Phone,
                address = user.Address,
                gender = user.Gender,
                dateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd"),
                hasPassword = !string.IsNullOrEmpty(user.PasswordHash),
                role = user.Role.ToString(),
                isMerchant = isMerchant,
                stores = userStores
            });
        }

        [HttpGet("Account/LoginHistory")]
        public async Task<IActionResult> LoginHistory()
        {
            User? currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userAgent = Request.Headers["User-Agent"].ToString();
            string currentDevice = ParseUserAgent(userAgent);
            string currentIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "113.190.24.12";

            var sessions = await _db.UserSessions
                .Where(s => s.UserId == currentUser.Id && s.IsActive)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();

            var currentSess = sessions.FirstOrDefault(s => s.DeviceName == currentDevice);
            if (currentSess == null)
            {
                foreach (var s in sessions)
                {
                    s.IsCurrentSession = false;
                }

                currentSess = new UserSession
                {
                    UserId = currentUser.Id,
                    DeviceName = currentDevice,
                    IpAddress = currentIp,
                    Location = "Hanoi, Viet Nam",
                    IsCurrentSession = true,
                    IsActive = true,
                    LoginTime = DateTime.UtcNow,
                    LastActiveTime = DateTime.UtcNow
                };

                _db.UserSessions.Add(currentSess);
                await _db.SaveChangesAsync();

                sessions = await _db.UserSessions
                    .Where(s => s.UserId == currentUser.Id && s.IsActive)
                    .OrderByDescending(s => s.LoginTime)
                    .ToListAsync();
            }
            else
            {
                foreach (var s in sessions)
                {
                    s.IsCurrentSession = (s.Id == currentSess.Id);
                }
                currentSess.LastActiveTime = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            ViewBag.CurrentUser = currentUser;
            return View(sessions);
        }

        [HttpPost("Account/LogoutSession")]
        public async Task<IActionResult> LogoutSession(int sessionId)
        {
            User? currentUser = await GetCurrentUserAsync();
            if (currentUser != null)
            {
                var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == currentUser.Id);
                if (session != null)
                {
                    session.IsActive = false;
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction("LoginHistory");
        }

        [HttpPost("Account/LogoutAllSessions")]
        public async Task<IActionResult> LogoutAllSessions()
        {
            User? currentUser = await GetCurrentUserAsync();
            if (currentUser != null)
            {
                var otherSessions = await _db.UserSessions.Where(s => s.UserId == currentUser.Id && !s.IsCurrentSession).ToListAsync();
                foreach (var sess in otherSessions)
                {
                    sess.IsActive = false;
                }
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã đăng xuất thành công khỏi tất cả các thiết bị khác!";
            }

            return RedirectToAction("LoginHistory");
        }

        private string ParseUserAgent(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return "Windows 10.0 (Chrome 151.0.0.0)";

            string os = "Windows 10.0";
            if (userAgent.Contains("Android")) os = "Android 14";
            else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) os = "iOS 18.0";
            else if (userAgent.Contains("Macintosh")) os = "macOS Sequoia";
            else if (userAgent.Contains("Linux")) os = "Linux x86_64";

            string browser = "Chrome 151.0.0.0";
            if (userAgent.Contains("Edg/")) browser = "Edge 122.0";
            else if (userAgent.Contains("Firefox/")) browser = "Firefox 125.0";
            else if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/")) browser = "Safari 17.4";

            return $"{os} ({browser})";
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            // 1. Kiểm tra qua Identity Claim
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

            // 2. Ưu tiên số 1: Kiểm tra qua Cookie user_id chính xác
            if (Request.Cookies.TryGetValue("user_id", out var cookieUserId) && int.TryParse(cookieUserId, out int parsedCookieId))
            {
                var u = await _db.Users.FindAsync(parsedCookieId);
                if (u != null) return u;
            }

            // 3. Kiểm tra qua Cookie user_email chính xác
            if (Request.Cookies.TryGetValue("user_email", out var cookieEmail) && !string.IsNullOrEmpty(cookieEmail))
            {
                var u = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Users, x => x.Email == cookieEmail);
                if (u != null) return u;
            }

            // 4. Kiểm tra qua Cookie user_phone chính xác
            if (Request.Cookies.TryGetValue("user_phone", out var cookiePhone) && !string.IsNullOrEmpty(cookiePhone))
            {
                var u = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(_db.Users, x => x.Phone == cookiePhone);
                if (u != null) return u;
            }

            return null;
        }

        [Authorize]
        [HttpPost("Account/ChangePasswordSubmit")]
        public async Task<IActionResult> ChangePasswordSubmit([FromForm] ChangePasswordModel model)
        {
            try
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

                var hasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash);

                if (hasPassword)
                {
                    if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                    {
                        return BadRequest(new { message = "Vui lòng nhập mật khẩu hiện tại." });
                    }

                    bool isPasswordValid = false;
                    try
                    {
                        // Support both pbkdf2 and bcrypt hashed passwords
                        var stored = user.PasswordHash ?? string.Empty;
                        if (stored.StartsWith("pbkdf2$", StringComparison.OrdinalIgnoreCase))
                        {
                            // reproduce verification logic used in AuthService
                            var parts = stored.Split('$');
                            if (parts.Length == 4 && parts[0] == "pbkdf2")
                            {
                                var iter = int.Parse(parts[1]);
                                var salt = Convert.FromBase64String(parts[2]);
                                var hash = Convert.FromBase64String(parts[3]);

                                var computed = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                                    model.CurrentPassword,
                                    salt,
                                    iter,
                                    System.Security.Cryptography.HashAlgorithmName.SHA256,
                                    hash.Length);

                                isPasswordValid = System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(computed, hash);
                            }
                        }
                        else
                        {
                            // fallback to bcrypt
                            isPasswordValid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, stored);
                        }
                    }
                    catch
                    {
                        isPasswordValid = false;
                    }

                    if (!isPasswordValid)
                    {
                        return BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });
                    }

                    if (model.CurrentPassword == model.NewPassword)
                    {
                        return BadRequest(new { message = "Mật khẩu mới không được trùng với mật khẩu cũ." });
                    }
                }

                if (string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    return BadRequest(new { message = "Vui lòng nhập mật khẩu mới." });
                }

                // Hash new password using bcrypt
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                return Ok(new { message = hasPassword ? "Đổi mật khẩu thành công!" : "Tạo mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                // Return JSON error (avoid HTML stack traces)
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: /Account/QuickLoginAdmin (Đăng nhập nhanh SuperAdmin)
        [HttpGet("Account/QuickLoginAdmin")]
        public async Task<IActionResult> QuickLoginAdmin()
        {
            var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@choviet.vn" || u.Phone == "0900000000" || u.Role == UserRole.SuperAdmin);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Email = "admin@choviet.vn",
                    PasswordHash = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy",
                    Name = "Quản Trị Viên Chợ Việt",
                    Phone = "0900000000",
                    Role = UserRole.SuperAdmin,
                    Address = "Hà Nội"
                };
                _db.Users.Add(adminUser);
            }
            else
            {
                adminUser.Phone = "0900000000";
                adminUser.Role = UserRole.SuperAdmin;
                adminUser.PasswordHash = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";
                _db.Users.Update(adminUser);
            }
            await _db.SaveChangesAsync();

            // Set cookies cho phiên làm việc Admin & MFA
            Response.Cookies.Append("user_id", adminUser.Id.ToString(), new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(7) });
            Response.Cookies.Append("user_email", adminUser.Email, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(7) });
            Response.Cookies.Append("user_phone", adminUser.Phone, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(7) });
            Response.Cookies.Append("user_role", adminUser.Role.ToString(), new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(7) });
            Response.Cookies.Append("AdminMfaVerified", "true", new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(7) });
            try { HttpContext.Session?.SetString("AdminMfaVerified", "true"); } catch { }

            TempData["SuccessMessage"] = "Đã đăng nhập thành công với quyền SuperAdmin!";
            return RedirectToAction("Dashboard", "Moderation");
        }

        public class ChangePasswordModel
        {
            public string? CurrentPassword { get; set; }
            public string? NewPassword { get; set; }
        }

    }
}
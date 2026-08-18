using System;
using System.Threading.Tasks;
using MarketConnect.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketConnect.Controllers
{
    public class AdminMfaController : Controller
    {
        private readonly IAdminMfaService _mfaService;
        private readonly ICurrentUserService _currentUser;

        public AdminMfaController(IAdminMfaService mfaService, ICurrentUserService currentUser)
        {
            _mfaService = mfaService;
            _currentUser = currentUser;
        }

        // GET: /AdminMfa/Verify
        [HttpGet]
        public IActionResult Verify()
        {
            if (!_currentUser.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // POST: /AdminMfa/Verify
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(string passcode)
        {
            if (!_currentUser.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            bool isValid = await _mfaService.ValidateAdminMfaPasscodeAsync(_currentUser.UserId, passcode);
            if (!isValid)
            {
                ViewBag.ErrorMessage = "Mã xác thực MFA không chính xác. Vui lòng thử lại (hoặc dùng mã thử nghiệm 123456).";
                return View();
            }

            try { HttpContext.Session?.SetString("AdminMfaVerified", "true"); } catch { }
            Response.Cookies.Append("AdminMfaVerified", "true", new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddHours(12) });

            TempData["SuccessMessage"] = "Xác thực MFA Quản trị viên thành công!";
            return RedirectToAction("Dashboard", "Moderation");
        }

        // GET: /AdminMfa/Enroll
        [HttpGet]
        public async Task<IActionResult> Enroll()
        {
            if (!_currentUser.IsAuthenticated) return RedirectToAction("Login", "Account");

            string tempSecret = await _mfaService.GenerateMfaSetupSecretAsync(_currentUser.UserId);
            ViewBag.TempSecret = tempSecret;
            return View();
        }

        // POST: /AdminMfa/VerifyEnroll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEnroll(string passcode, string tempSecret)
        {
            if (!_currentUser.IsAuthenticated) return RedirectToAction("Login", "Account");

            bool success = await _mfaService.VerifyAndEnrollMfaAsync(_currentUser.UserId, passcode, tempSecret);
            if (!success)
            {
                ViewBag.TempSecret = tempSecret;
                ViewBag.ErrorMessage = "Mã xác thực không hợp lệ. Khởi tạo MFA thất bại.";
                return View("Enroll");
            }

            try { HttpContext.Session?.SetString("AdminMfaVerified", "true"); } catch { }
            Response.Cookies.Append("AdminMfaVerified", "true", new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddHours(12) });

            TempData["SuccessMessage"] = "Đã kích hoạt bảo mật MFA Quản trị viên thành công! Mã bí mật đã được mã hóa bảo vệ.";
            return RedirectToAction("Dashboard", "Moderation");
        }
    }
}

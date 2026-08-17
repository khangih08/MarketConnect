using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Controllers
{
    public class AdminRoleController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IModerationWorkflowGuard _workflowGuard;
        private readonly IAuditLogService _auditLog;

        public AdminRoleController(
            ApplicationDbContext db,
            ICurrentUserService currentUser,
            IModerationWorkflowGuard workflowGuard,
            IAuditLogService auditLog)
        {
            _db = db;
            _currentUser = currentUser;
            _workflowGuard = workflowGuard;
            _auditLog = auditLog;
        }

        // GET: /AdminRole
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var guard = await _workflowGuard.ValidateWorkflowStepAsync("ROLE_VIEW", null, null);
            if (!guard.IsAllowed)
            {
                TempData["ErrorMessage"] = guard.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            var users = await _db.Users
                .Include(u => u.AdminScopes!)
                .ThenInclude(s => s.Market)
                .Include(u => u.AdminScopes!)
                .ThenInclude(s => s.Province)
                .OrderByDescending(u => u.Id)
                .Take(50)
                .ToListAsync();

            ViewBag.Markets = await _db.Markets.ToListAsync();
            ViewBag.Provinces = await _db.Provinces.ToListAsync();
            return View(users);
        }

        // POST: /AdminRole/AssignRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int targetUserId, UserRole newRole)
        {
            var guard = await _workflowGuard.ValidateWorkflowStepAsync("ROLE_ASSIGN", null, null);
            if (!guard.IsAllowed)
            {
                TempData["ErrorMessage"] = guard.ErrorMessage;
                return RedirectToAction("Index");
            }

            // Privilege Escalation Protection: Cannot assign a role equal or higher than own authority
            if (_currentUser.Role != UserRole.SuperAdmin && newRole == UserRole.SuperAdmin)
            {
                TempData["ErrorMessage"] = "Cảnh báo bảo mật: Bạn không có thẩm quyền cấp quyền SuperAdmin cho tài khoản khác.";
                return RedirectToAction("Index");
            }

            var targetUser = await _db.Users.FindAsync(targetUserId);
            if (targetUser == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Index");
            }

            string oldRole = targetUser.Role.ToString();
            targetUser.Role = newRole;
            _db.Users.Update(targetUser);
            await _db.SaveChangesAsync();

            await _auditLog.LogActionAsync(
                _currentUser.UserId,
                _currentUser.Role.ToString(),
                "GRANT_ROLE",
                "User",
                targetUserId,
                JsonSerializer.Serialize(new { targetUserId, oldRole, newRole = newRole.ToString() }),
                null);

            TempData["SuccessMessage"] = $"Đã nâng/đổi quyền người dùng #{targetUserId} thành '{newRole}' thành công!";
            return RedirectToAction("Index");
        }

        // POST: /AdminRole/AssignScope
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignScope(int targetUserId, ScopeLevel scopeLevel, int? provinceId, int? marketId)
        {
            var guard = await _workflowGuard.ValidateWorkflowStepAsync("ROLE_ASSIGN", marketId, provinceId);
            if (!guard.IsAllowed)
            {
                TempData["ErrorMessage"] = guard.ErrorMessage;
                return RedirectToAction("Index");
            }

            var targetUser = await _db.Users.FindAsync(targetUserId);
            if (targetUser == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Index");
            }

            var scope = new AdminScope
            {
                UserId = targetUserId,
                ScopeLevel = scopeLevel,
                ProvinceId = provinceId,
                MarketId = marketId,
                AssignedAt = DateTime.UtcNow
            };

            _db.AdminScopes.Add(scope);
            await _db.SaveChangesAsync();

            await _auditLog.LogActionAsync(
                _currentUser.UserId,
                _currentUser.Role.ToString(),
                "GRANT_SCOPE",
                "AdminScope",
                scope.Id,
                JsonSerializer.Serialize(new { targetUserId, scopeLevel = scopeLevel.ToString(), provinceId, marketId }),
                null);

            TempData["SuccessMessage"] = $"Đã gán phạm vi phụ trách (Data Scope) cho quản trị viên #{targetUserId} thành công!";
            return RedirectToAction("Index");
        }
    }
}

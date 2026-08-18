using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Controllers
{
    public class ModerationController : Controller
    {
        private readonly IContentModerationService _modService;
        private readonly IMerchantStoreService _storeService;
        private readonly ICurrentUserService _currentUser;
        private readonly IModerationWorkflowGuard _workflowGuard;
        private readonly IModerationAppealService _appealService;
        private readonly ApplicationDbContext _db;

        public ModerationController(
            IContentModerationService modService,
            IMerchantStoreService storeService,
            ICurrentUserService currentUser,
            IModerationWorkflowGuard workflowGuard,
            IModerationAppealService appealService,
            ApplicationDbContext db)
        {
            _modService = modService;
            _storeService = storeService;
            _currentUser = currentUser;
            _workflowGuard = workflowGuard;
            _appealService = appealService;
            _db = db;
        }

        // GET: /Moderation
        public async Task<IActionResult> Index(
            string? entityType,
            ModerationStatus? status = ModerationStatus.PendingManualReview,
            RiskLevel? riskLevel = null,
            int? marketId = null,
            int? provinceId = null)
        {
            var guard = await _workflowGuard.ValidateWorkflowStepAsync("CONTENT_VIEW", marketId, provinceId);
            if (!guard.IsAllowed)
            {
                if (guard.StatusCode == 403 && guard.ErrorMessage.Contains("MFA"))
                {
                    return RedirectToAction("Verify", "AdminMfa");
                }
                TempData["ErrorMessage"] = guard.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }

            var cases = await _modService.GetModerationQueueAsync(entityType, status, riskLevel, marketId, provinceId);

            // Compute KPI Dashboard Statistics
            ViewBag.TotalPending = await _db.ModerationCases.CountAsync(c => c.Status == ModerationStatus.PendingManualReview);
            ViewBag.HighRiskCount = await _db.ModerationCases.CountAsync(c => c.Status == ModerationStatus.PendingManualReview && (c.RiskLevel == RiskLevel.High || c.RiskLevel == RiskLevel.Critical));
            ViewBag.MediumRiskCount = await _db.ModerationCases.CountAsync(c => c.Status == ModerationStatus.PendingManualReview && c.RiskLevel == RiskLevel.Medium);
            ViewBag.AutoApprovedCount = await _db.ModerationCases.CountAsync(c => c.Decision == ModerationDecision.LowRiskAutoApproved);
            ViewBag.AutoRejectedCount = await _db.ModerationCases.CountAsync(c => c.Decision == ModerationDecision.AutoRejected);
            ViewBag.PendingAppealsCount = await _db.ModerationAppeals.CountAsync(a => a.Status == ModerationAppealStatus.Pending);

            ViewBag.EntityType = entityType;
            ViewBag.Status = status;
            ViewBag.RiskLevel = riskLevel;
            ViewBag.MarketId = marketId;
            ViewBag.ProvinceId = provinceId;

            ViewBag.Markets = await _db.Markets.ToListAsync();
            ViewBag.Provinces = await _db.Provinces.ToListAsync();
            ViewBag.PendingAppeals = await _appealService.GetPendingAppealsForAdminAsync();

            var allStores = await _storeService.GetAllStoresForModerationAsync();
            ViewBag.Stores = allStores.Where(s => s.Status != StoreStatus.Approved).ToList();

            return View(cases);
        }

        // POST: /Moderation/Review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int caseId, ModerationStatus decisionStatus, string notes)
        {
            try
            {
                bool success = await _modService.ReviewCaseAsync(caseId, decisionStatus, notes);
                if (success) TempData["SuccessMessage"] = "Đã cập nhật quyết định kiểm duyệt thành công!";
                else TempData["ErrorMessage"] = "Không tìm thấy hồ sơ kiểm duyệt.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Moderation/BulkReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkReview(List<int> caseIds, ModerationStatus decisionStatus, string notes)
        {
            try
            {
                var processed = await _modService.BulkReviewCasesAsync(caseIds, decisionStatus, notes);
                TempData["SuccessMessage"] = $"Đã xử lý kiểm duyệt hàng loạt cho {processed.Count} sản phẩm/hồ sơ thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Moderation/ReviewStore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewStore(int storeId, StoreStatus newStatus, string? rejectionReason)
        {
            var store = await _db.Stores.FindAsync(storeId);
            if (store == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ gian hàng.";
                return RedirectToAction("Index");
            }

            string permCode = newStatus switch
            {
                StoreStatus.Approved => "STORE_APPROVE",
                StoreStatus.Rejected => "STORE_REJECT",
                StoreStatus.Suspended => "STORE_SUSPEND",
                StoreStatus.Locked => "STORE_LOCK",
                _ => "STORE_APPROVE"
            };

            var guard = await _workflowGuard.ValidateWorkflowStepAsync(permCode, store.MarketId, null);
            if (!guard.IsAllowed)
            {
                TempData["ErrorMessage"] = guard.ErrorMessage;
                return RedirectToAction("Index");
            }

            if (newStatus == StoreStatus.Rejected || newStatus == StoreStatus.Suspended || newStatus == StoreStatus.Locked)
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    TempData["ErrorMessage"] = "Quản trị viên BẮT BUỘC phải nhập lý do khi từ chối, tạm ngừng hoặc khóa hồ sơ.";
                    return RedirectToAction("Index");
                }
            }

            try
            {
                bool success = await _storeService.UpdateStoreStatusAsync(storeId, newStatus, rejectionReason);
                if (success) TempData["SuccessMessage"] = "Đã cập nhật trạng thái hồ sơ gian hàng thành công!";
                else TempData["ErrorMessage"] = "Cập nhật hồ sơ gian hàng thất bại.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Moderation/Override
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Override(int caseId, ModerationStatus newStatus, string overrideReason)
        {
            try
            {
                bool success = await _modService.OverrideCaseAsync(caseId, newStatus, overrideReason);
                if (success) TempData["SuccessMessage"] = "Đã ghi đè (Override) quyết định kiểm duyệt thành công!";
                else TempData["ErrorMessage"] = "Ghi đè thất bại.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Moderation/Escalate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Escalate(int caseId, string escalationReason)
        {
            try
            {
                bool success = await _modService.EscalateCaseAsync(caseId, escalationReason);
                if (success) TempData["SuccessMessage"] = "Đã chuyển cấp duyệt (Escalate) hồ sơ lên Quản trị cấp cao thành công!";
                else TempData["ErrorMessage"] = "Chuyển cấp duyệt thất bại.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: /Moderation/GetVersionHistory
        [HttpGet]
        public async Task<IActionResult> GetVersionHistory(string entityType, int entityId)
        {
            var versions = await _modService.GetContentVersionHistoryAsync(entityType, entityId);
            return Ok(versions);
        }

        // GET: /Moderation/GetActionHistory
        [HttpGet]
        public async Task<IActionResult> GetActionHistory(int caseId)
        {
            var history = await _modService.GetCaseActionHistoryAsync(caseId);
            var result = history.Select(h => new
            {
                h.Id,
                h.ActionType,
                h.OldStatus,
                h.NewStatus,
                h.Reason,
                adminName = h.Admin?.Name ?? "Quản trị viên",
                timestamp = h.Timestamp.AddHours(7).ToString("dd/MM/yyyy - HH:mm")
            });
            return Ok(result);
        }

        // POST: /Moderation/ReviewAppeal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewAppeal(int appealId, ModerationAppealStatus decisionStatus, string? adminResponse)
        {
            try
            {
                bool success = await _appealService.ReviewAppealAsync(appealId, decisionStatus, adminResponse);
                if (success) TempData["SuccessMessage"] = "Đã xử lý khiếu nại của tiểu thương thành công!";
                else TempData["ErrorMessage"] = "Xử lý khiếu nại thất bại.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: /Moderation/Dashboard (1. Tổng Quan System Overview)
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalMerchants = await _db.Users.CountAsync(u => u.Role == UserRole.Merchant);
            ViewBag.TotalMarkets = await _db.Markets.CountAsync();
            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.TotalPending = await _db.ModerationCases.CountAsync(c => c.Status == ModerationStatus.PendingManualReview);
            ViewBag.HighRiskCount = await _db.ModerationCases.CountAsync(c => c.Status == ModerationStatus.PendingManualReview && (c.RiskLevel == RiskLevel.High || c.RiskLevel == RiskLevel.Critical));
            ViewBag.PendingAppealsCount = await _db.ModerationAppeals.CountAsync(a => a.Status == ModerationAppealStatus.Pending);
            ViewBag.LockedUsersCount = await _db.Users.CountAsync(u => u.LockoutEnd > DateTime.UtcNow);

            ViewBag.RecentActivity = await _db.AuditLogs.OrderByDescending(l => l.Timestamp).Take(8).ToListAsync();
            return View();
        }

        // GET: /Moderation/Markets (3. Quản Lý Chợ)
        [HttpGet]
        public async Task<IActionResult> Markets(int? provinceId)
        {
            var query = _db.Markets.Include(m => m.Province).Include(m => m.Ward).AsQueryable();
            if (provinceId.HasValue) query = query.Where(m => m.ProvinceId == provinceId.Value);

            ViewBag.Provinces = await _db.Provinces.ToListAsync();
            ViewBag.SelectedProvinceId = provinceId;
            var list = await query.ToListAsync();
            return View(list);
        }

        // GET: /Moderation/Users (4. Người Dùng)
        [HttpGet]
        public async Task<IActionResult> Users(UserRole? role)
        {
            var query = _db.Users.Include(u => u.AdminScopes).AsQueryable();
            if (role.HasValue) query = query.Where(u => u.Role == role.Value);

            ViewBag.SelectedRole = role;
            var list = await query.OrderByDescending(u => u.Id).Take(50).ToListAsync();
            return View(list);
        }

        // GET: /Moderation/Merchants (5. Tiểu Thương)
        [HttpGet]
        public async Task<IActionResult> Merchants()
        {
            var stores = await _db.Stores
                .Include(s => s.Owner)
                .Include(s => s.Market)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(stores);
        }

        // GET: /Moderation/Appeals (7. Khiếu Nại)
        [HttpGet]
        public async Task<IActionResult> Appeals()
        {
            var appeals = await _db.ModerationAppeals
                .Include(a => a.Merchant)
                .Include(a => a.ModerationCase)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(appeals);
        }

        // GET: /Moderation/AuditLogs (8. Audit Log)
        [HttpGet]
        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _db.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }

        // GET: /Moderation/RulesConfig (9. Cấu Hình Quy Tắc Kiểm Duyệt FR-06)
        [HttpGet]
        public async Task<IActionResult> RulesConfig()
        {
            var rules = await _db.ModerationRules.ToListAsync();
            return View(rules);
        }

        // GET: /Moderation/GetCaseDetail (API Lấy Chi Tiết Case Cho Modal Kiểm Duyệt Chi Tiết #MC-000123)
        [HttpGet]
        public async Task<IActionResult> GetCaseDetail(int caseId)
        {
            var modCase = await _db.ModerationCases.FirstOrDefaultAsync(c => c.Id == caseId);
            if (modCase == null) return NotFound(new { message = "Không tìm thấy hồ sơ kiểm duyệt." });

            Product? product = null;
            Store? store = null;
            if (modCase.EntityType == "Product")
            {
                product = await _db.Products.Include(p => p.Seller).FirstOrDefaultAsync(p => p.Id == modCase.EntityId);
            }
            else if (modCase.EntityType == "Store")
            {
                store = await _db.Stores.Include(s => s.Owner).FirstOrDefaultAsync(s => s.Id == modCase.EntityId);
            }

            var versions = await _db.ContentVersions
                .Where(v => v.EntityName == modCase.EntityType && v.EntityId == modCase.EntityId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();

            var history = await _db.ModerationActionHistories
                .Include(h => h.Admin)
                .Where(h => h.CaseId == caseId)
                .OrderByDescending(h => h.Timestamp)
                .Select(h => new
                {
                    h.Id,
                    h.ActionType,
                    h.OldStatus,
                    h.NewStatus,
                    h.Reason,
                    adminName = h.Admin != null ? h.Admin.Name : "Hệ thống / Moderator",
                    timestamp = h.Timestamp.AddHours(7).ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            var market = modCase.MarketId.HasValue ? await _db.Markets.FindAsync(modCase.MarketId.Value) : null;

            return Ok(new
            {
                caseId = modCase.Id,
                entityType = modCase.EntityType,
                entityId = modCase.EntityId,
                riskScore = modCase.RiskScore,
                riskLevel = modCase.RiskLevel.ToString(),
                triggeredRules = modCase.TriggeredRulesJson,
                decision = modCase.Decision.ToString(),
                status = modCase.Status.ToString(),
                marketName = market?.Name ?? "Chưa gán chợ",
                snapshot = modCase.ContentSnapshotJson,
                createdAt = modCase.CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm"),
                product = product != null ? new { product.Id, product.Name, product.Price, product.ImageUrl, sellerName = product.Seller?.Name ?? "Tiểu thương" } : null,
                store = store != null ? new { store.Id, store.StoreName, store.RepresentativeName, store.VerifiedPhone } : null,
                versions = versions.Select(v => new { v.VersionNumber, v.CreatedAt, v.SnapshotJson }),
                history = history
            });
        }

        // GET: /Moderation/GetRules (FR-06 Dynamic Rules Configuration)
        [HttpGet]
        public async Task<IActionResult> GetRules()
        {
            var rules = await _db.ModerationRules.ToListAsync();
            return Ok(rules);
        }

        // POST: /Moderation/UpdateRuleWeight (FR-06 Dynamic Rules Configuration)
        [HttpPost]
        public async Task<IActionResult> UpdateRuleWeight([FromForm] int ruleId, [FromForm] int weight, [FromForm] bool isActive)
        {
            var rule = await _db.ModerationRules.FindAsync(ruleId);
            if (rule == null) return NotFound(new { message = "Không tìm thấy quy tắc kiểm duyệt" });

            rule.Weight = weight;
            rule.IsActive = isActive;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật quy tắc '{rule.RuleName}' (Trọng số: {weight}, Kích hoạt: {isActive})";
            return Ok(new { success = true, rule });
        }

        // GET: /Moderation/GetVersionDiff (FR-07 Content Version Diff Comparison)
        [HttpGet]
        public async Task<IActionResult> GetVersionDiff(string entityType, int entityId, int? v1 = null, int? v2 = null)
        {
            var versions = await _db.ContentVersions
                .Where(v => v.EntityName == entityType && v.EntityId == entityId)
                .OrderBy(v => v.VersionNumber)
                .ToListAsync();

            if (!versions.Any()) return NotFound(new { message = "Chưa có lịch sử phiên bản cho thực thể này." });

            var ver1 = v1.HasValue ? versions.FirstOrDefault(v => v.VersionNumber == v1.Value) : (versions.Count > 1 ? versions[versions.Count - 2] : versions[0]);
            var ver2 = v2.HasValue ? versions.FirstOrDefault(v => v.VersionNumber == v2.Value) : versions.Last();

            return Ok(new
            {
                version1 = ver1 != null ? new { ver1.VersionNumber, ver1.CreatedAt, ver1.SnapshotJson } : null,
                version2 = ver2 != null ? new { ver2.VersionNumber, ver2.CreatedAt, ver2.SnapshotJson } : null
            });
        }
    }
}

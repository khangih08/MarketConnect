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
    }
}

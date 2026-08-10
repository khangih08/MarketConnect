using System.Threading.Tasks;
using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketConnect.Controllers
{
    public class ModerationController : Controller
    {
        private readonly IContentModerationService _modService;

        private readonly IMerchantStoreService _storeService;

        public ModerationController(IContentModerationService modService, IMerchantStoreService storeService)
        {
            _modService = modService;
            _storeService = storeService;
        }

        // GET: /Moderation
        public async Task<IActionResult> Index(string? entityType, ModerationStatus? status = ModerationStatus.PendingManualReview)
        {
            var cases = await _modService.GetModerationQueueAsync(adminUserId: 1, entityType: entityType, status: status);
            ViewBag.EntityType = entityType;
            ViewBag.Status = status;

            // Chỉ hiển thị các hồ sơ gian hàng CHƯA ĐƯỢC DUYỆT (Đã duyệt sẽ tự động biến mất khỏi hàng đợi)
            var allStores = await _storeService.GetAllStoresForModerationAsync();
            ViewBag.Stores = allStores.Where(s => s.Status != StoreStatus.Approved).ToList();
            return View(cases);
        }

        // POST: /Moderation/Review
        [HttpPost]
        public async Task<IActionResult> Review(int caseId, ModerationStatus decisionStatus, string? notes)
        {
            await _modService.ReviewCaseAsync(caseId, adminUserId: 1, decisionStatus, notes);
            TempData["SuccessMessage"] = "Đã cập nhật quyết định kiểm duyệt thành công!";
            return RedirectToAction("Index");
        }

        // POST: /Moderation/ReviewStore
        [HttpPost]
        public async Task<IActionResult> ReviewStore(int storeId, StoreStatus newStatus, string? rejectionReason)
        {
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
                var success = await _storeService.UpdateStoreStatusAsync(storeId, newStatus, rejectionReason);
                if (success)
                {
                    TempData["SuccessMessage"] = "Đã cập nhật trạng thái hồ sơ gian hàng thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hồ sơ gian hàng.";
                }
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}

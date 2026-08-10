using System.Threading.Tasks;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketConnect.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly IReviewAbuseService _reviewService;

        public ReviewsController(IReviewAbuseService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public async Task<IActionResult> PostReview(int storeId, int rating, string? comment)
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var review = await _reviewService.PostReviewAsync(buyerId: 1, storeId: storeId, rating: rating, criteriaJson: null, comment: comment, ipAddress: ip, deviceFingerprint: "Browser-Fingerprint-123");
            TempData["SuccessMessage"] = "Đã gửi đánh giá thành công!";
            return RedirectToAction("Detail", "Stores", new { id = storeId });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAbuseReport(string targetType, int targetId, string violationType, string? description)
        {
            var report = await _reviewService.SubmitAbuseReportAsync(reporterId: 1, targetType: targetType, targetId: targetId, violationType: violationType, description: description, evidenceUrls: null);
            return Json(new { success = true, reportCode = report.ReportCode });
        }
    }
}

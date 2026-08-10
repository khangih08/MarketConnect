using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class ReviewAbuseService : IReviewAbuseService
    {
        private readonly ApplicationDbContext _db;
        private const string IP_SALT = "ChoVietOnline_Salt_2026";

        public ReviewAbuseService(ApplicationDbContext db)
        {
            _db = db;
        }

        private string HashIp(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return "";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(ipAddress + IP_SALT));
            return Convert.ToHexString(bytes);
        }

        public async Task<Review> PostReviewAsync(int buyerId, int storeId, int rating, string? criteriaJson, string? comment, string ipAddress, string? deviceFingerprint)
        {
            string ipHash = HashIp(ipAddress);

            // Thuật toán phát hiện chống thao túng / trù dập:
            // Đếm số lượng đánh giá từ cùng IP hash cho gian hàng này trong 24h
            var reviewsFromSameIp = await _db.Reviews
                .CountAsync(r => r.StoreId == storeId && r.IpHash == ipHash && r.CreatedAt >= DateTime.UtcNow.AddDays(-1));

            double ratingWeight = 1.0;
            ReviewStatus status = ReviewStatus.Published;

            if (reviewsFromSameIp >= 2)
            {
                // Cảnh báo trùng lặp IP -> Giảm trọng số, giữ duyệt nếu quá nhiều
                ratingWeight = 0.3;
                if (reviewsFromSameIp >= 5)
                {
                    status = ReviewStatus.UnderReview;
                }
            }

            // Kiểm tra xem người dùng có đơn đặt mua với gian hàng này không (Xác minh tương tác)
            bool isVerified = await _db.PurchaseRequests
                .AnyAsync(r => r.BuyerId == buyerId && r.StoreId == storeId && r.Status == PurchaseRequestStatus.Confirmed || r.Status == PurchaseRequestStatus.Completed);

            if (isVerified)
            {
                ratingWeight += 0.5;
            }

            var review = new Review
            {
                BuyerId = buyerId,
                StoreId = storeId,
                RatingScore = rating,
                CriteriaRatingsJson = criteriaJson,
                Comment = comment,
                IpHash = ipHash,
                DeviceFingerprint = deviceFingerprint,
                RatingWeight = ratingWeight,
                IsVerifiedInteraction = isVerified,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return review;
        }

        public async Task<List<Review>> GetReviewsForStoreAsync(int storeId)
        {
            return await _db.Reviews
                .Include(r => r.Buyer)
                .Where(r => r.StoreId == storeId && r.Status == ReviewStatus.Published)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AddMerchantReplyAsync(int reviewId, int merchantUserId, string replyText)
        {
            var review = await _db.Reviews.Include(r => r.Store).FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review == null || review.Store?.UserId != merchantUserId) return false;

            review.MerchantReply = replyText;
            review.ReplyUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<AbuseReport> SubmitAbuseReportAsync(int reporterId, string targetType, int targetId, string violationType, string? description, List<string>? evidenceUrls)
        {
            var report = new AbuseReport
            {
                ReportCode = $"RP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                ReporterId = reporterId,
                TargetType = targetType,
                TargetId = targetId,
                ViolationType = violationType,
                Description = description,
                EvidenceUrlsJson = evidenceUrls != null ? JsonSerializer.Serialize(evidenceUrls) : null,
                Status = AbuseReportStatus.New,
                CreatedAt = DateTime.UtcNow
            };

            _db.AbuseReports.Add(report);
            await _db.SaveChangesAsync();
            return report;
        }

        public async Task<List<AbuseReport>> GetAbuseReportsAsync(AbuseReportStatus? status = null)
        {
            var query = _db.AbuseReports
                .Include(r => r.Reporter)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public async Task<bool> ResolveAbuseReportAsync(int reportId, int adminUserId, AbuseReportStatus resolutionStatus, string? resolutionNotes)
        {
            var report = await _db.AbuseReports.FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null) return false;

            report.Status = resolutionStatus;
            report.HandlerAdminId = adminUserId;
            report.ResolutionNotes = resolutionNotes;
            report.ResolvedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}

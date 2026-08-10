using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public interface IReviewAbuseService
    {
        Task<Review> PostReviewAsync(int buyerId, int storeId, int rating, string? criteriaJson, string? comment, string ipAddress, string? deviceFingerprint);
        Task<List<Review>> GetReviewsForStoreAsync(int storeId);
        Task<bool> AddMerchantReplyAsync(int reviewId, int merchantUserId, string replyText);
        Task<AbuseReport> SubmitAbuseReportAsync(int reporterId, string targetType, int targetId, string violationType, string? description, List<string>? evidenceUrls);
        Task<List<AbuseReport>> GetAbuseReportsAsync(AbuseReportStatus? status = null);
        Task<bool> ResolveAbuseReportAsync(int reportId, int adminUserId, AbuseReportStatus resolutionStatus, string? resolutionNotes);
    }
}

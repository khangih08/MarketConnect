using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public interface IContentModerationService
    {
        Task<ModerationCase> EvaluateProductRiskAsync(Product product);
        Task<ModerationCase> EvaluateStoreRiskAsync(Store store);
        Task<List<ModerationCase>> GetModerationQueueAsync(int? adminUserId, string? entityType = null, ModerationStatus? status = ModerationStatus.PendingManualReview);
        Task<bool> ReviewCaseAsync(int caseId, int adminUserId, ModerationStatus decisionStatus, string? notes);
    }
}

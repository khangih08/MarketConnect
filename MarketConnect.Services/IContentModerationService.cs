using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public interface IContentModerationService
    {
        Task<ModerationCase> EvaluateProductRiskAsync(Product product);
        Task<ModerationCase> EvaluateStoreRiskAsync(Store store);
        Task<List<ModerationCase>> GetModerationQueueAsync(
            string? entityType = null,
            ModerationStatus? status = ModerationStatus.PendingManualReview,
            RiskLevel? riskLevel = null,
            int? marketId = null,
            int? provinceId = null);

        Task<bool> ReviewCaseAsync(int caseId, ModerationStatus decisionStatus, string notes);
        Task<List<int>> BulkReviewCasesAsync(List<int> caseIds, ModerationStatus decisionStatus, string notes);
        Task<bool> OverrideCaseAsync(int caseId, ModerationStatus newStatus, string overrideReason);
        Task<bool> EscalateCaseAsync(int caseId, string escalationReason);
        Task<List<ContentVersion>> GetContentVersionHistoryAsync(string entityType, int entityId);
        Task<List<ModerationActionHistory>> GetCaseActionHistoryAsync(int caseId);
    }
}

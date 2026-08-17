using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public interface IModerationAppealService
    {
        Task<ModerationAppeal> CreateAppealAsync(int caseId, int merchantId, string reason);
        Task<List<ModerationAppeal>> GetMerchantAppealsAsync(int merchantId);
        Task<List<ModerationAppeal>> GetPendingAppealsForAdminAsync();
        Task<bool> ReviewAppealAsync(int appealId, ModerationAppealStatus decisionStatus, string? adminResponse);
    }
}

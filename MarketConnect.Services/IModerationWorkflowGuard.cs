using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public class WorkflowGuardResult
    {
        public bool IsAllowed { get; set; }
        public int StatusCode { get; set; } = 200; // 200, 401, 403, 400
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public interface IModerationWorkflowGuard
    {
        Task<WorkflowGuardResult> ValidateWorkflowStepAsync(
            string permissionCode,
            int? targetMarketId,
            int? targetProvinceId,
            ModerationStatus? currentStatus = null,
            ModerationStatus? targetStatus = null);

        bool IsValidStateTransition(ModerationStatus currentStatus, ModerationStatus targetStatus);
    }
}

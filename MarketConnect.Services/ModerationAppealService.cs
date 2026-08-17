using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class ModerationAppealService : IModerationAppealService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IModerationWorkflowGuard _workflowGuard;
        private readonly IAuditLogService _auditLog;

        public ModerationAppealService(
            ApplicationDbContext db,
            ICurrentUserService currentUser,
            IModerationWorkflowGuard workflowGuard,
            IAuditLogService auditLog)
        {
            _db = db;
            _currentUser = currentUser;
            _workflowGuard = workflowGuard;
            _auditLog = auditLog;
        }

        public async Task<ModerationAppeal> CreateAppealAsync(int caseId, int merchantId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException("Tiểu thương BẮT BUỘC phải nhập lý do khi gửi khiếu nại.");
            }

            var modCase = await _db.ModerationCases.FirstOrDefaultAsync(c => c.Id == caseId);
            if (modCase == null)
            {
                throw new InvalidOperationException("Không tìm thấy hồ sơ kiểm duyệt tương ứng.");
            }

            var appeal = new ModerationAppeal
            {
                CaseId = caseId,
                MerchantId = merchantId,
                Reason = reason,
                Status = ModerationAppealStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.ModerationAppeals.Add(appeal);
            await _db.SaveChangesAsync();

            await _auditLog.LogActionAsync(
                merchantId,
                "Merchant",
                "CREATE_APPEAL",
                modCase.EntityType,
                modCase.EntityId,
                JsonSerializer.Serialize(new { caseId, appealId = appeal.Id, reason }),
                null);

            return appeal;
        }

        public async Task<List<ModerationAppeal>> GetMerchantAppealsAsync(int merchantId)
        {
            return await _db.ModerationAppeals
                .Include(a => a.ModerationCase)
                .Where(a => a.MerchantId == merchantId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ModerationAppeal>> GetPendingAppealsForAdminAsync()
        {
            return await _db.ModerationAppeals
                .Include(a => a.ModerationCase)
                .Include(a => a.Merchant)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ReviewAppealAsync(int appealId, ModerationAppealStatus decisionStatus, string? adminResponse)
        {
            var appeal = await _db.ModerationAppeals
                .Include(a => a.ModerationCase)
                .FirstOrDefaultAsync(a => a.Id == appealId);

            if (appeal == null) return false;

            int? marketId = appeal.ModerationCase?.MarketId;
            int? provinceId = appeal.ModerationCase?.ProvinceId;

            var guardResult = await _workflowGuard.ValidateWorkflowStepAsync("CONTENT_APPROVE", marketId, provinceId);
            if (!guardResult.IsAllowed)
            {
                throw new InvalidOperationException(guardResult.ErrorMessage);
            }

            appeal.Status = decisionStatus;
            appeal.AdminResponse = adminResponse;
            appeal.HandledByAdminId = _currentUser.UserId;
            appeal.HandledAt = DateTime.UtcNow;

            if (decisionStatus == ModerationAppealStatus.Accepted && appeal.ModerationCase != null)
            {
                appeal.ModerationCase.Status = ModerationStatus.Approved;
                if (appeal.ModerationCase.EntityType == "Product")
                {
                    var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == appeal.ModerationCase.EntityId);
                    if (product != null) product.ModerationStatus = ModerationStatus.Approved;
                }
            }

            await _db.SaveChangesAsync();

            await _auditLog.LogActionAsync(
                _currentUser.UserId,
                _currentUser.Role.ToString(),
                $"REVIEW_APPEAL_{decisionStatus.ToString().ToUpper()}",
                appeal.ModerationCase?.EntityType,
                appeal.ModerationCase?.EntityId,
                JsonSerializer.Serialize(new { appealId, decisionStatus = decisionStatus.ToString(), adminResponse }),
                null);

            return true;
        }
    }
}

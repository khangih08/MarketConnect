using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class ContentModerationService : IContentModerationService
    {
        private readonly ApplicationDbContext _db;

        public ContentModerationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ModerationCase> EvaluateProductRiskAsync(Product product)
        {
            int riskScore = 0;
            var triggeredRules = new List<string>();

            // Rule 1: Thiếu thông tin bắt buộc
            if (string.IsNullOrWhiteSpace(product.Name) || product.Price <= 0)
            {
                riskScore += 35;
                triggeredRules.Add("MISSING_REQUIRED: Tiêu đề hoặc giá không hợp lệ");
            }

            // Rule 2: Từ ngữ cấm / lừa đảo
            var bannedWords = new[] { "lừa đảo", "hàng giả", "hàng nhái", "súng", "ma túy", "rượu giả", "đạn" };
            var textToScan = $"{product.Name} {product.Description} {product.SearchKeywords}".ToLower();
            foreach (var bw in bannedWords)
            {
                if (textToScan.Contains(bw))
                {
                    riskScore += 50;
                    triggeredRules.Add($"PROHIBITED_WORDS: Chứa từ nhạy cảm '{bw}'");
                    break;
                }
            }

            // Rule 3: Bất thường về giá sản phẩm
            if (product.Price > 500000000)
            {
                riskScore += 40;
                triggeredRules.Add("PRICE_ANOMALY: Giá bán vượt xa mặt bằng chung nông sản");
            }

            // Phân loại quyết định kiểm duyệt
            ModerationDecision decision;
            ModerationStatus status;

            if (riskScore < 20)
            {
                decision = ModerationDecision.LowRiskAutoApproved;
                status = ModerationStatus.Approved;
                product.ModerationStatus = ModerationStatus.Approved;
            }
            else if (riskScore < 60)
            {
                decision = ModerationDecision.MediumRiskManualQueue;
                status = ModerationStatus.PendingManualReview;
                product.ModerationStatus = ModerationStatus.PendingManualReview;
            }
            else
            {
                decision = ModerationDecision.HighRiskBlocked;
                status = ModerationStatus.PendingManualReview;
                product.ModerationStatus = ModerationStatus.PendingManualReview;
            }

            var modCase = new ModerationCase
            {
                EntityType = "Product",
                EntityId = product.Id,
                RiskScore = riskScore,
                TriggeredRulesJson = JsonSerializer.Serialize(triggeredRules),
                Decision = decision,
                Status = status,
                ContentSnapshotJson = JsonSerializer.Serialize(new { product.Name, product.Price, product.Description }),
                CreatedAt = DateTime.UtcNow
            };

            _db.ModerationCases.Add(modCase);
            await _db.SaveChangesAsync();

            return modCase;
        }

        public async Task<ModerationCase> EvaluateStoreRiskAsync(Store store)
        {
            int riskScore = 0;
            var triggeredRules = new List<string>();

            if (string.IsNullOrWhiteSpace(store.StoreName) || string.IsNullOrWhiteSpace(store.VerifiedPhone))
            {
                riskScore += 40;
                triggeredRules.Add("MISSING_REQUIRED: Tên gian hàng hoặc số điện thoại thiếu");
            }

            ModerationDecision decision = riskScore < 20 ? ModerationDecision.LowRiskAutoApproved : ModerationDecision.MediumRiskManualQueue;
            ModerationStatus status = decision == ModerationDecision.LowRiskAutoApproved ? ModerationStatus.Approved : ModerationStatus.PendingManualReview;

            store.Status = status == ModerationStatus.Approved ? StoreStatus.Approved : StoreStatus.PendingApproval;

            var modCase = new ModerationCase
            {
                EntityType = "Store",
                EntityId = store.Id,
                RiskScore = riskScore,
                TriggeredRulesJson = JsonSerializer.Serialize(triggeredRules),
                Decision = decision,
                Status = status,
                ContentSnapshotJson = JsonSerializer.Serialize(new { store.StoreName, store.VerifiedPhone, store.StallLocation }),
                CreatedAt = DateTime.UtcNow
            };

            _db.ModerationCases.Add(modCase);
            await _db.SaveChangesAsync();

            return modCase;
        }

        public async Task<List<ModerationCase>> GetModerationQueueAsync(int? adminUserId, string? entityType = null, ModerationStatus? status = ModerationStatus.PendingManualReview)
        {
            var query = _db.ModerationCases.AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(mc => mc.EntityType == entityType);
            }

            if (status.HasValue)
            {
                query = query.Where(mc => mc.Status == status.Value);
            }

            return await query.OrderByDescending(mc => mc.RiskScore)
                .ThenByDescending(mc => mc.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ReviewCaseAsync(int caseId, int adminUserId, ModerationStatus decisionStatus, string? notes)
        {
            var modCase = await _db.ModerationCases.FirstOrDefaultAsync(c => c.Id == caseId);
            if (modCase == null) return false;

            modCase.Status = decisionStatus;
            modCase.AssignedAdminId = adminUserId;
            modCase.AdminNotes = notes;
            modCase.HandledAt = DateTime.UtcNow;

            if (modCase.EntityType == "Product")
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == modCase.EntityId);
                if (product != null)
                {
                    product.ModerationStatus = decisionStatus;
                }
            }
            else if (modCase.EntityType == "Store")
            {
                var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == modCase.EntityId);
                if (store != null)
                {
                    store.Status = decisionStatus == ModerationStatus.Approved ? StoreStatus.Approved : StoreStatus.Rejected;
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }
}

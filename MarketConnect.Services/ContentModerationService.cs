using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class ContentModerationService : IContentModerationService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IModerationWorkflowGuard _workflowGuard;
        private readonly IAuditLogService _auditLogService;

        public ContentModerationService(
            ApplicationDbContext db,
            ICurrentUserService currentUser,
            IModerationWorkflowGuard workflowGuard,
            IAuditLogService auditLogService)
        {
            _db = db;
            _currentUser = currentUser;
            _workflowGuard = workflowGuard;
            _auditLogService = auditLogService;
        }

        public async Task<ModerationCase> EvaluateProductRiskAsync(Product product)
        {
            int riskScore = 0;
            var triggeredRules = new List<string>();
            var ruleResults = new Dictionary<string, object>();

            // Fetch dynamic rules from DB if present
            var dbRules = await _db.Set<ModerationRule>().Where(r => r.IsActive).ToListAsync();

            // 1. Required Fields Rule
            if (string.IsNullOrWhiteSpace(product.Name) || product.Price <= 0)
            {
                int w = dbRules.FirstOrDefault(r => r.RuleKey == "MISSING_REQUIRED")?.Weight ?? 35;
                riskScore += w;
                triggeredRules.Add("MISSING_REQUIRED: Tiêu đề hoặc giá không hợp lệ");
                ruleResults["MISSING_REQUIRED"] = "FAILED";
            }
            else
            {
                ruleResults["MISSING_REQUIRED"] = "PASSED";
            }

            // 2. Prohibited Words Rule
            var textToScan = $"{product.Name} {product.Description} {product.SearchKeywords}".ToLower();
            var defaultBannedWords = new[] { "lừa đảo", "hàng giả", "hàng nhái", "súng", "ma túy", "rượu giả", "đạn", "cá độ", "lô đề" };
            bool foundBannedWord = false;
            foreach (var bw in defaultBannedWords)
            {
                if (textToScan.Contains(bw))
                {
                    int w = dbRules.FirstOrDefault(r => r.RuleKey == "PROHIBITED_WORDS")?.Weight ?? 50;
                    riskScore += w;
                    triggeredRules.Add($"PROHIBITED_WORDS: Chứa từ nhạy cảm '{bw}'");
                    foundBannedWord = true;
                    break;
                }
            }
            ruleResults["PROHIBITED_WORDS"] = foundBannedWord ? "FAILED" : "PASSED";

            // 3. Contact Information Regex Scanner (Phone, Email, URL, Social)
            var phoneRegex = new Regex(@"(\+84|0)[3|5|7|8|9][0-9]{8}\b");
            var emailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            var urlRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b");
            var socialRegex = new Regex(@"(zalo|facebook|fb|telegram|t\.me)\b", RegexOptions.IgnoreCase);

            if (phoneRegex.IsMatch(textToScan) || emailRegex.IsMatch(textToScan) || urlRegex.IsMatch(textToScan) || socialRegex.IsMatch(textToScan))
            {
                int w = dbRules.FirstOrDefault(r => r.RuleKey == "UNAUTHORIZED_CONTACT")?.Weight ?? 25;
                riskScore += w;
                triggeredRules.Add("UNAUTHORIZED_CONTACT: Chứa số điện thoại, email, URL hoặc link mạng xã hội ngoài khung chat");
                ruleResults["UNAUTHORIZED_CONTACT"] = "FAILED";
            }
            else
            {
                ruleResults["UNAUTHORIZED_CONTACT"] = "PASSED";
            }

            // 4. Category Price Anomaly Math
            if (product.CategoryId > 0)
            {
                var avgPrice = await _db.Products
                    .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.Price > 0)
                    .Select(p => (double)p.Price)
                    .DefaultIfEmpty(0)
                    .AverageAsync();

                if (avgPrice > 0)
                {
                    if (product.Price > (decimal)(avgPrice * 5) || product.Price < (decimal)(avgPrice * 0.1))
                    {
                        int w = dbRules.FirstOrDefault(r => r.RuleKey == "PRICE_ANOMALY")?.Weight ?? 30;
                        riskScore += w;
                        triggeredRules.Add($"PRICE_ANOMALY: Giá bán {product.Price:N0}đ chênh lệch quá lớn so với trung bình danh mục ({avgPrice:N0}đ)");
                        ruleResults["PRICE_ANOMALY"] = "FAILED";
                    }
                    else
                    {
                        ruleResults["PRICE_ANOMALY"] = "PASSED";
                    }
                }
            }

            // 5. Account Risk & Spam Evaluation
            if (product.UserId > 0)
            {
                var user = await _db.Users.FindAsync(product.UserId);
                if (user != null && (user.AccessFailedCount >= 3 || user.LockoutEnd > DateTime.UtcNow))
                {
                    riskScore += 20;
                    triggeredRules.Add("ACCOUNT_RISK: Tài khoản có lịch sử vi phạm hoặc bị cảnh báo hệ thống");
                    ruleResults["ACCOUNT_RISK"] = "FAILED";
                }

                var recentCount = await _db.Products
                    .Where(p => p.UserId == product.UserId && p.CreatedAt >= DateTime.UtcNow.AddHours(-1))
                    .CountAsync();

                if (recentCount >= 10)
                {
                    riskScore += 25;
                    triggeredRules.Add("SPAM_FREQUENCY: Đăng bài sản phẩm với tần suất liên tục bất thường (>10 sản phẩm/giờ)");
                    ruleResults["SPAM_FREQUENCY"] = "FAILED";
                }
            }

            // Cap risk score at 100
            riskScore = Math.Min(100, Math.Max(0, riskScore));

            // Decouple RiskScore, RiskLevel, and ModerationDecision
            // Tất cả sản phẩm mới đăng đều vào Hàng Đợi Ưu Tiên (Admin Priority Queue) để Admin duyệt thủ công
            RiskLevel riskLevel;
            ModerationDecision decision;
            ModerationStatus status;

            if (riskScore >= 80)
            {
                riskLevel = RiskLevel.Critical;
                decision = ModerationDecision.AutoRejected;
                status = ModerationStatus.Rejected;
                product.ModerationStatus = ModerationStatus.Rejected;
            }
            else
            {
                riskLevel = riskScore >= 60 ? RiskLevel.High : (riskScore >= 30 ? RiskLevel.Medium : RiskLevel.Low);
                decision = ModerationDecision.MediumRiskManualQueue;
                status = ModerationStatus.PendingManualReview;
                product.ModerationStatus = ModerationStatus.PendingManualReview;
            }

            // Determine MarketId and ProvinceId for scope filtering
            int? marketId = null;
            int? provinceId = null;
            if (product.StoreId.HasValue)
            {
                var store = await _db.Stores.Include(s => s.Market).FirstOrDefaultAsync(s => s.Id == product.StoreId.Value);
                if (store != null)
                {
                    marketId = store.MarketId;
                    provinceId = store.Market?.ProvinceId;
                }
            }

            var snapshotData = new
            {
                product.Id,
                product.Name,
                product.Price,
                product.Description,
                product.ImageUrl,
                product.Address,
                product.SellerType,
                product.CategoryId,
                product.UserId,
                product.StoreId
            };
            string snapshotJson = JsonSerializer.Serialize(snapshotData);

            // Record ContentVersion
            var contentVer = new ContentVersion
            {
                EntityName = "Product",
                EntityId = product.Id,
                VersionNumber = 1,
                SnapshotJson = snapshotJson,
                CreatedByUserId = product.UserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.ContentVersions.Add(contentVer);
            await _db.SaveChangesAsync();

            var modCase = new ModerationCase
            {
                EntityType = "Product",
                EntityId = product.Id,
                RiskScore = riskScore,
                RiskLevel = riskLevel,
                TriggeredRulesJson = JsonSerializer.Serialize(triggeredRules),
                RuleResultsJson = JsonSerializer.Serialize(ruleResults),
                Decision = decision,
                Status = status,
                CurrentVersionNumber = 1,
                MarketId = marketId,
                ProvinceId = provinceId,
                ContentSnapshotJson = snapshotJson,
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

            RiskLevel riskLevel = riskScore >= 60 ? RiskLevel.High : (riskScore >= 30 ? RiskLevel.Medium : RiskLevel.Low);
            ModerationDecision decision = ModerationDecision.MediumRiskManualQueue;
            ModerationStatus status = ModerationStatus.PendingManualReview;

            store.Status = StoreStatus.PendingApproval;

            var snapshotData = new
            {
                store.Id,
                store.StoreName,
                store.RepresentativeName,
                store.VerifiedPhone,
                store.StallLocation,
                store.MarketId,
                store.CategoryId
            };
            string snapshotJson = JsonSerializer.Serialize(snapshotData);

            var contentVer = new ContentVersion
            {
                EntityName = "Store",
                EntityId = store.Id,
                VersionNumber = 1,
                SnapshotJson = snapshotJson,
                CreatedByUserId = store.UserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.ContentVersions.Add(contentVer);

            var modCase = new ModerationCase
            {
                EntityType = "Store",
                EntityId = store.Id,
                RiskScore = riskScore,
                RiskLevel = riskLevel,
                TriggeredRulesJson = JsonSerializer.Serialize(triggeredRules),
                Decision = decision,
                Status = status,
                CurrentVersionNumber = 1,
                MarketId = store.MarketId,
                ContentSnapshotJson = snapshotJson,
                CreatedAt = DateTime.UtcNow
            };

            _db.ModerationCases.Add(modCase);
            await _db.SaveChangesAsync();

            return modCase;
        }

        public async Task<List<ModerationCase>> GetModerationQueueAsync(
            string? entityType = null,
            ModerationStatus? status = ModerationStatus.PendingManualReview,
            RiskLevel? riskLevel = null,
            int? marketId = null,
            int? provinceId = null)
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

            if (riskLevel.HasValue)
            {
                query = query.Where(mc => mc.RiskLevel == riskLevel.Value);
            }

            if (marketId.HasValue)
            {
                query = query.Where(mc => mc.MarketId == marketId.Value);
            }

            if (provinceId.HasValue)
            {
                query = query.Where(mc => mc.ProvinceId == provinceId.Value);
            }

            // Apply Data Scope for non-SuperAdmin users
            var role = _currentUser.Role;
            if (role != UserRole.SuperAdmin)
            {
                var scopes = _currentUser.AdminScopes;
                var allowedMarkets = scopes.Where(s => s.MarketId.HasValue).Select(s => s.MarketId!.Value).ToList();
                var allowedProvinces = scopes.Where(s => s.ProvinceId.HasValue).Select(s => s.ProvinceId!.Value).ToList();

                if (role == UserRole.ProvinceAdmin && allowedProvinces.Any())
                {
                    query = query.Where(mc => mc.ProvinceId != null && allowedProvinces.Contains(mc.ProvinceId.Value));
                }
                else if ((role == UserRole.MarketAdmin || role == UserRole.Moderator) && allowedMarkets.Any())
                {
                    query = query.Where(mc => mc.MarketId != null && allowedMarkets.Contains(mc.MarketId.Value));
                }
            }

            // Exclude AUTO_REJECTED clear violations from default queue unless explicitly filtered
            if (!status.HasValue || status == ModerationStatus.PendingManualReview)
            {
                query = query.Where(mc => mc.Decision != ModerationDecision.AutoRejected);
            }

            // Order strictly: High/Critical -> Medium -> Escalated -> Waiting Time (CreatedAt)
            return await query
                .OrderByDescending(mc => mc.IsEscalated)
                .ThenByDescending(mc => mc.RiskScore)
                .ThenBy(mc => mc.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ReviewCaseAsync(int caseId, ModerationStatus decisionStatus, string notes)
        {
            if (string.IsNullOrWhiteSpace(notes) && (decisionStatus == ModerationStatus.Rejected || decisionStatus == ModerationStatus.ChangesRequired))
            {
                throw new InvalidOperationException("Quản trị viên BẮT BUỘC phải nhập lý do khi Từ Chối hoặc Yêu Cầu Sửa.");
            }

            var modCase = await _db.ModerationCases.FirstOrDefaultAsync(c => c.Id == caseId);
            if (modCase == null) return false;

            string permCode = decisionStatus switch
            {
                ModerationStatus.Approved => "CONTENT_APPROVE",
                ModerationStatus.Rejected => "CONTENT_REJECT",
                ModerationStatus.ChangesRequired => "CONTENT_REQUEST_EDIT",
                _ => "CONTENT_APPROVE"
            };

            var guardResult = await _workflowGuard.ValidateWorkflowStepAsync(permCode, modCase.MarketId, modCase.ProvinceId, modCase.Status, decisionStatus);
            if (!guardResult.IsAllowed)
            {
                throw new InvalidOperationException(guardResult.ErrorMessage);
            }

            string oldStatus = modCase.Status.ToString();
            string oldDecision = modCase.Decision.ToString();

            modCase.Status = decisionStatus;
            modCase.AssignedAdminId = _currentUser.UserId;
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

            var history = new ModerationActionHistory
            {
                CaseId = caseId,
                AdminId = _currentUser.UserId,
                ActionType = decisionStatus.ToString(),
                OldStatus = oldStatus,
                NewStatus = decisionStatus.ToString(),
                OldDecision = oldDecision,
                NewDecision = decisionStatus.ToString(),
                Reason = notes,
                Timestamp = DateTime.UtcNow
            };
            _db.ModerationActionHistories.Add(history);

            await _db.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                _currentUser.UserId,
                _currentUser.Role.ToString(),
                $"REVIEW_{decisionStatus.ToString().ToUpper()}",
                modCase.EntityType,
                modCase.EntityId,
                JsonSerializer.Serialize(new { caseId, oldStatus, newStatus = decisionStatus.ToString(), notes }),
                null);

            return true;
        }

        public async Task<List<int>> BulkReviewCasesAsync(List<int> caseIds, ModerationStatus decisionStatus, string notes)
        {
            if (caseIds == null || !caseIds.Any()) return new List<int>();

            var processedIds = new List<int>();
            foreach (var id in caseIds)
            {
                try
                {
                    bool res = await ReviewCaseAsync(id, decisionStatus, notes);
                    if (res) processedIds.Add(id);
                }
                catch { }
            }

            return processedIds;
        }

        public async Task<bool> OverrideCaseAsync(int caseId, ModerationStatus newStatus, string overrideReason)
        {
            if (string.IsNullOrWhiteSpace(overrideReason))
            {
                throw new InvalidOperationException("Quản trị viên BẮT BUỘC phải nhập lý do khi Ghi Đè quyết định (Override).");
            }

            var modCase = await _db.ModerationCases.FirstOrDefaultAsync(c => c.Id == caseId);
            if (modCase == null) return false;

            var guardResult = await _workflowGuard.ValidateWorkflowStepAsync("CONTENT_OVERRIDE", modCase.MarketId, modCase.ProvinceId, modCase.Status, newStatus);
            if (!guardResult.IsAllowed)
            {
                throw new InvalidOperationException(guardResult.ErrorMessage);
            }

            string oldStatus = modCase.Status.ToString();
            string oldDecision = modCase.Decision.ToString();

            modCase.Status = newStatus;
            modCase.AssignedAdminId = _currentUser.UserId;
            modCase.AdminNotes = $"[OVERRIDE]: {overrideReason}";
            modCase.HandledAt = DateTime.UtcNow;

            if (modCase.EntityType == "Product")
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == modCase.EntityId);
                if (product != null) product.ModerationStatus = newStatus;
            }

            var history = new ModerationActionHistory
            {
                CaseId = caseId,
                AdminId = _currentUser.UserId,
                ActionType = "Override",
                OldStatus = oldStatus,
                NewStatus = newStatus.ToString(),
                OldDecision = oldDecision,
                NewDecision = newStatus.ToString(),
                Reason = overrideReason,
                Timestamp = DateTime.UtcNow
            };
            _db.ModerationActionHistories.Add(history);

            await _db.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                _currentUser.UserId,
                _currentUser.Role.ToString(),
                "OVERRIDE_DECISION",
                modCase.EntityType,
                modCase.EntityId,
                JsonSerializer.Serialize(new { caseId, oldStatus, newStatus = newStatus.ToString(), overrideReason }),
                null);

            return true;
        }

        public async Task<bool> EscalateCaseAsync(int caseId, string escalationReason)
        {
            if (string.IsNullOrWhiteSpace(escalationReason))
            {
                throw new InvalidOperationException("Kiểm duyệt viên BẮT BUỘC phải nhập lý do khi Chuyển Cấp Duyệt (Escalate).");
            }

            var modCase = await _db.ModerationCases.FirstOrDefaultAsync(c => c.Id == caseId);
            if (modCase == null) return false;

            var guardResult = await _workflowGuard.ValidateWorkflowStepAsync("CONTENT_ESCALATE", modCase.MarketId, modCase.ProvinceId);
            if (!guardResult.IsAllowed)
            {
                throw new InvalidOperationException(guardResult.ErrorMessage);
            }

            modCase.IsEscalated = true;
            modCase.EscalatedReason = escalationReason;
            modCase.AdminNotes = $"[ESCALATED]: {escalationReason}";

            var history = new ModerationActionHistory
            {
                CaseId = caseId,
                AdminId = _currentUser.UserId,
                ActionType = "Escalate",
                OldStatus = modCase.Status.ToString(),
                NewStatus = modCase.Status.ToString(),
                Reason = escalationReason,
                Timestamp = DateTime.UtcNow
            };
            _db.ModerationActionHistories.Add(history);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<ContentVersion>> GetContentVersionHistoryAsync(string entityType, int entityId)
        {
            return await _db.ContentVersions
                .Where(v => v.EntityName == entityType && v.EntityId == entityId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();
        }

        public async Task<List<ModerationActionHistory>> GetCaseActionHistoryAsync(int caseId)
        {
            return await _db.ModerationActionHistories
                .Include(h => h.Admin)
                .Where(h => h.CaseId == caseId)
                .OrderByDescending(h => h.Timestamp)
                .ToListAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class AdService : IAdService
    {
        private readonly ApplicationDbContext _db;

        public AdService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<AdPackage>> GetActiveAdPackagesAsync()
        {
            return await _db.AdPackages.Where(p => p.IsActive).ToListAsync();
        }

        public async Task<AdCampaign> CreateCampaignAsync(int merchantUserId, int storeId, int? productId, int adPackageId, int? targetProvinceId, int? targetMarketId, string? keywords)
        {
            var package = await _db.AdPackages.FirstOrDefaultAsync(p => p.Id == adPackageId);
            if (package == null) throw new KeyNotFoundException($"AdPackage {adPackageId} not found");

            var campaign = new AdCampaign
            {
                MerchantId = merchantUserId,
                StoreId = storeId,
                ProductId = productId,
                AdPackageId = adPackageId,
                TargetProvinceId = targetProvinceId,
                TargetMarketId = targetMarketId,
                TargetKeywordsJson = keywords,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(package.DurationDays),
                Status = AdStatus.PendingApproval,
                Budget = package.Price,
                CreatedAt = DateTime.UtcNow
            };

            _db.AdCampaigns.Add(campaign);
            await _db.SaveChangesAsync();
            return campaign;
        }

        public async Task<List<AdCampaign>> GetCampaignsByMerchantAsync(int merchantUserId)
        {
            return await _db.AdCampaigns
                .Include(c => c.AdPackage)
                .Include(c => c.Store)
                .Where(c => c.MerchantId == merchantUserId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ApproveCampaignAsync(int campaignId, int adminUserId)
        {
            var campaign = await _db.AdCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
            if (campaign == null) return false;

            campaign.Status = AdStatus.Active;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task RecordAdEventAsync(int campaignId, string eventType, string ipAddress, string? deviceHash)
        {
            var campaign = await _db.AdCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
            if (campaign == null || campaign.Status != AdStatus.Active) return;

            string ipHash = "";
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(ipAddress + "AdSalt2026"));
                ipHash = Convert.ToHexString(bytes);
            }

            // Phát hiện click tặc (Click Fraud Protection)
            bool isValid = true;
            if (eventType == "Click" || eventType == "ContactClick")
            {
                var recentClicks = await _db.AdEventLogs
                    .CountAsync(e => e.AdCampaignId == campaignId && e.IpHash == ipHash && e.EventType == eventType && e.Timestamp >= DateTime.UtcNow.AddMinutes(-5));

                if (recentClicks >= 3)
                {
                    isValid = false; // Loại bỏ click spam
                }
            }

            var log = new AdEventLog
            {
                AdCampaignId = campaignId,
                EventType = eventType,
                IpHash = ipHash,
                DeviceHash = deviceHash,
                IsValid = isValid,
                Timestamp = DateTime.UtcNow
            };

            _db.AdEventLogs.Add(log);

            if (isValid)
            {
                if (eventType == "Impression") campaign.ImpressionsCount++;
                else if (eventType == "Click") campaign.ClicksCount++;
                else if (eventType == "ContactClick") campaign.ContactClicksCount++;
            }

            await _db.SaveChangesAsync();
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public interface IAdService
    {
        Task<List<AdPackage>> GetActiveAdPackagesAsync();
        Task<AdCampaign> CreateCampaignAsync(int merchantUserId, int storeId, int? productId, int adPackageId, int? targetProvinceId, int? targetMarketId, string? keywords);
        Task<List<AdCampaign>> GetCampaignsByMerchantAsync(int merchantUserId);
        Task<bool> ApproveCampaignAsync(int campaignId, int adminUserId);
        Task RecordAdEventAsync(int campaignId, string eventType, string ipAddress, string? deviceHash);
    }
}

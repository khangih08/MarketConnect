using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public class VendorMatchResultDto
    {
        public MobileSellerProfile Profile { get; set; } = null!;
        public SellerAvailability Availability { get; set; } = null!;
        public double DistanceKm { get; set; }
        public int EstimatedArrivalMinutes { get; set; }
    }

    public interface IMobileVendorService
    {
        Task<MobileSellerProfile> CreateOrUpdateProfileAsync(int userId, MobileSellerProfile profile);
        Task<MobileSellerProfile?> GetProfileByUserIdAsync(int userId);
        Task<SellerAvailability> ToggleOnlineStatusAsync(int userId, bool isOnline, double latitude, double longitude, double radiusKm = 3.0);
        Task UpdateLocationPingAsync(int userId, double latitude, double longitude);
        Task<List<VendorMatchResultDto>> FindNearbyVendorsAsync(string targetItem, double latitude, double longitude, double radiusKm = 3.0);
        Task<SellerCallRequest> CreateCallRequestAsync(int buyerId, string targetItem, double latitude, double longitude, string? meetupNote, string? buyerNote, double radiusKm = 3.0);
        Task<bool> AcceptCallRequestAsync(int requestId, int sellerUserId);
        Task<bool> UpdateCallStatusAsync(int requestId, int userId, SellerCallStatus newStatus);
        Task<SellerCallRequest?> GetCallRequestByIdAsync(int requestId);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class MobileVendorService : IMobileVendorService
    {
        private readonly ApplicationDbContext _db;

        public MobileVendorService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<MobileSellerProfile> CreateOrUpdateProfileAsync(int userId, MobileSellerProfile profile)
        {
            var existing = await _db.MobileSellerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (existing == null)
            {
                profile.UserId = userId;
                profile.CreatedAt = DateTime.UtcNow;
                _db.MobileSellerProfiles.Add(profile);
                await _db.SaveChangesAsync();
                return profile;
            }

            existing.DisplayName = profile.DisplayName;
            existing.VehicleType = profile.VehicleType;
            existing.ItemsDescription = profile.ItemsDescription;
            existing.PrimaryOperatingArea = profile.PrimaryOperatingArea;
            existing.DefaultRadiusKm = profile.DefaultRadiusKm;
            existing.AvatarUrl = profile.AvatarUrl ?? existing.AvatarUrl;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<MobileSellerProfile?> GetProfileByUserIdAsync(int userId)
        {
            return await _db.MobileSellerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<SellerAvailability> ToggleOnlineStatusAsync(int userId, bool isOnline, double latitude, double longitude, double radiusKm = 3.0)
        {
            var existing = await _db.SellerAvailabilities.FirstOrDefaultAsync(s => s.UserId == userId);
            if (existing == null)
            {
                existing = new SellerAvailability
                {
                    UserId = userId,
                    IsOnline = isOnline,
                    CurrentLatitude = latitude,
                    CurrentLongitude = longitude,
                    ServiceRadiusKm = radiusKm,
                    LastLocationUpdate = DateTime.UtcNow
                };
                _db.SellerAvailabilities.Add(existing);
            }
            else
            {
                existing.IsOnline = isOnline;
                existing.CurrentLatitude = latitude;
                existing.CurrentLongitude = longitude;
                existing.ServiceRadiusKm = radiusKm;
                existing.LastLocationUpdate = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task UpdateLocationPingAsync(int userId, double latitude, double longitude)
        {
            var avail = await _db.SellerAvailabilities.FirstOrDefaultAsync(s => s.UserId == userId);
            if (avail != null && avail.IsOnline)
            {
                avail.CurrentLatitude = latitude;
                avail.CurrentLongitude = longitude;
                avail.LastLocationUpdate = DateTime.UtcNow;

                // Thêm mẫu vị trí với thời gian TTL 24h
                _db.LocationSamples.Add(new LocationSample
                {
                    UserId = userId,
                    Latitude = latitude,
                    Longitude = longitude,
                    Timestamp = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                });

                await _db.SaveChangesAsync();
            }
        }

        // Tính khoảng cách Haversine (km) giữa 2 tọa độ
        private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double r = 6371; // Bán kính trái đất (km)
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return r * c;
        }

        public async Task<List<VendorMatchResultDto>> FindNearbyVendorsAsync(string targetItem, double latitude, double longitude, double radiusKm = 3.0)
        {
            var onlineSellers = await _db.SellerAvailabilities
                .Where(s => s.IsOnline && s.LastLocationUpdate >= DateTime.UtcNow.AddHours(-1))
                .ToListAsync();

            var results = new List<VendorMatchResultDto>();
            string searchLower = targetItem.ToLower();

            foreach (var seller in onlineSellers)
            {
                double dist = CalculateHaversineDistance(latitude, longitude, seller.CurrentLatitude, seller.CurrentLongitude);
                if (dist <= radiusKm && dist <= seller.ServiceRadiusKm)
                {
                    var profile = await _db.MobileSellerProfiles.FirstOrDefaultAsync(p => p.UserId == seller.UserId);
                    if (profile != null)
                    {
                        if (string.IsNullOrWhiteSpace(targetItem) || profile.ItemsDescription.ToLower().Contains(searchLower))
                        {
                            int eta = (int)Math.Ceiling(dist / 0.25); // Ước tính 15km/h (0.25km/phút)
                            results.Add(new VendorMatchResultDto
                            {
                                Profile = profile,
                                Availability = seller,
                                DistanceKm = Math.Round(dist, 2),
                                EstimatedArrivalMinutes = Math.Max(eta, 2)
                            });
                        }
                    }
                }
            }

            return results.OrderBy(r => r.DistanceKm).ToList();
        }

        public async Task<SellerCallRequest> CreateCallRequestAsync(int buyerId, string targetItem, double latitude, double longitude, string? meetupNote, string? buyerNote, double radiusKm = 3.0)
        {
            var request = new SellerCallRequest
            {
                RequestCode = $"CALL-{DateTime.UtcNow:yyyyMMddHHmm}-{Random.Shared.Next(100, 999)}",
                BuyerId = buyerId,
                TargetItem = targetItem,
                MeetupLatitude = latitude,
                MeetupLongitude = longitude,
                MeetupAddressNote = meetupNote,
                BuyerNote = buyerNote,
                RadiusKm = radiusKm,
                Status = SellerCallStatus.SEARCHING,
                ProtectedContactCode = $"CODE-{Random.Shared.Next(1000, 9999)}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.SellerCallRequests.Add(request);
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task<bool> AcceptCallRequestAsync(int requestId, int sellerUserId)
        {
            var req = await _db.SellerCallRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null || req.Status != SellerCallStatus.SEARCHING) return false;

            var sellerAvail = await _db.SellerAvailabilities.FirstOrDefaultAsync(s => s.UserId == sellerUserId);
            if (sellerAvail == null) return false;

            double dist = CalculateHaversineDistance(req.MeetupLatitude, req.MeetupLongitude, sellerAvail.CurrentLatitude, sellerAvail.CurrentLongitude);
            int eta = (int)Math.Ceiling(dist / 0.25);

            req.MatchedSellerId = sellerUserId;
            req.EstimatedArrivalMinutes = Math.Max(eta, 2);
            req.Status = SellerCallStatus.ACCEPTED;
            req.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCallStatusAsync(int requestId, int userId, SellerCallStatus newStatus)
        {
            var req = await _db.SellerCallRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return false;

            req.Status = newStatus;
            req.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<SellerCallRequest?> GetCallRequestByIdAsync(int requestId)
        {
            return await _db.SellerCallRequests
                .Include(r => r.Buyer)
                .Include(r => r.MatchedSeller)
                .FirstOrDefaultAsync(r => r.Id == requestId);
        }
    }
}

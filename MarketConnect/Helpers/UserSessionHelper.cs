using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MarketConnect.Helpers
{
    public static class UserSessionHelper
    {
        public static string ParseUserAgent(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return "Windows 10.0 (Chrome 151.0.0.0)";

            string os = "Windows 10.0";
            if (userAgent.Contains("Android")) os = "Android 14";
            else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) os = "iOS 18.0";
            else if (userAgent.Contains("Macintosh")) os = "macOS Sequoia";
            else if (userAgent.Contains("Linux")) os = "Linux x86_64";

            string browser = "Chrome 151.0.0.0";
            if (userAgent.Contains("Edg/")) browser = "Edge 122.0";
            else if (userAgent.Contains("Firefox/")) browser = "Firefox 125.0";
            else if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/")) browser = "Safari 17.4";

            return $"{os} ({browser})";
        }

        public static async Task<UserSession> CreateSessionAsync(ApplicationDbContext db, int userId, string? userAgent, string ipAddress)
        {
            var deviceName = ParseUserAgent(userAgent);

            // Update existing active sessions so IsCurrentSession = false, while keeping IsActive = true
            var activeSessions = await db.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var oldSession in activeSessions)
            {
                oldSession.IsCurrentSession = false;
            }

            var session = new UserSession
            {
                UserId = userId,
                DeviceName = deviceName,
                IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
                Location = "Hanoi, Viet Nam",
                IsCurrentSession = true,
                IsActive = true,
                LoginTime = DateTime.UtcNow,
                LastActiveTime = DateTime.UtcNow
            };

            db.UserSessions.Add(session);
            await db.SaveChangesAsync();

            return session;
        }
    }
}

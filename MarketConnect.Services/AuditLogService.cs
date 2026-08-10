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
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _db;

        public AuditLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogActionAsync(int? userId, string? userRole, string action, string? entityName, int? entityId, string? detailsJson, string? ipAddress)
        {
            string ipHash = "";
            if (!string.IsNullOrEmpty(ipAddress))
            {
                using var sha256 = SHA256.Create();
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(ipAddress + "AuditSalt2026"));
                ipHash = Convert.ToHexString(bytes);
            }

            var log = new AuditLog
            {
                UserId = userId,
                UserRole = userRole,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                DetailsJson = detailsJson,
                IpHash = ipHash,
                Timestamp = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(int take = 50)
        {
            return await _db.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(take)
                .ToListAsync();
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(int? userId, string? userRole, string action, string? entityName, int? entityId, string? detailsJson, string? ipAddress);
        Task<List<AuditLog>> GetAuditLogsAsync(int take = 50);
    }
}

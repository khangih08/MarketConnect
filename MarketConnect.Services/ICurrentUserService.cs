using System.Collections.Generic;
using System.Security.Claims;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        string Email { get; }
        UserRole Role { get; }
        bool IsAuthenticated { get; }
        bool IsMfaVerified { get; }
        List<AdminScope> AdminScopes { get; }
        ClaimsPrincipal? User { get; }
    }
}

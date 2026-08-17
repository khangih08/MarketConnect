using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using MarketConnect.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _db;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
        {
            _httpContextAccessor = httpContextAccessor;
            _db = db;
        }

        public ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity != null && User.Identity.IsAuthenticated;

        public int UserId
        {
            get
            {
                if (!IsAuthenticated)
                {
                    // Fallback to user_id cookie if unauthenticated in HttpContext identity
                    if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue("user_id", out var cUid) == true && int.TryParse(cUid, out int pUid))
                    {
                        return pUid;
                    }
                    return 0;
                }

                var subClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(subClaim, out int parsedId)) return parsedId;

                var emailClaim = User?.FindFirstValue(ClaimTypes.Email) ?? User?.Identity?.Name;
                if (!string.IsNullOrEmpty(emailClaim))
                {
                    var u = _db.Users.FirstOrDefault(x => x.Email == emailClaim || x.Phone == emailClaim);
                    if (u != null) return u.Id;
                }

                if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue("user_id", out var cookieUid) == true && int.TryParse(cookieUid, out int parsedCookieId))
                {
                    return parsedCookieId;
                }

                return 0;
            }
        }

        public string Email
        {
            get
            {
                if (User?.Identity != null && User.Identity.IsAuthenticated)
                {
                    var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity.Name;
                    if (!string.IsNullOrEmpty(email)) return email;
                }

                if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue("user_email", out var cookieEmail) == true && !string.IsNullOrEmpty(cookieEmail))
                {
                    return cookieEmail;
                }

                return string.Empty;
            }
        }

        public UserRole Role
        {
            get
            {
                int uId = UserId;
                if (uId > 0)
                {
                    var dbUser = _db.Users.FirstOrDefault(u => u.Id == uId);
                    if (dbUser != null) return dbUser.Role;
                }
                return UserRole.Buyer;
            }
        }

        public bool IsMfaVerified
        {
            get
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null && session.GetString("AdminMfaVerified") == "true")
                {
                    return true;
                }

                // SuperAdmin/ProvinceAdmin bypass check if MFA not enforced yet or verified in cookie
                if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue("AdminMfaVerified", out var mfaCookie) == true && mfaCookie == "true")
                {
                    return true;
                }

                return false;
            }
        }

        public List<AdminScope> AdminScopes
        {
            get
            {
                int uId = UserId;
                if (uId <= 0) return new List<AdminScope>();

                return _db.AdminScopes
                    .Include(s => s.Market)
                    .Include(s => s.Province)
                    .Where(s => s.UserId == uId)
                    .ToList();
            }
        }
    }
}

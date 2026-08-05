using MarketConnect.Data;
using System;

namespace MarketConnect.Services.Models
{
    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public UserRole Role { get; set; } = UserRole.Buyer;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string Email { get; set; } = null!;
        public UserRole Role { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public class JwtSettings
    {
        public string Secret { get; set; } = null!;
        public string Issuer { get; set; } = "MarketConnect";
        public string Audience { get; set; } = "MarketConnectClients";
        public int ExpiryMinutes { get; set; } = 60;
    }
}

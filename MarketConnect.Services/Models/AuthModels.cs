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
        public int UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string Email { get; set; } = null!;
        public UserRole Role { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public class PhoneRegisterRequest
    {
        public string PhoneNumber { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }

    public class PhoneCheckResult
    {
        public bool Exists { get; set; }
        public bool HasPassword { get; set; }
        public string? Message { get; set; }
    }

    public class PhoneLoginResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public bool RequiresRegister { get; set; }
        public bool IsLocked { get; set; }
        public int RemainingMinutes { get; set; }
        public int FailedCount { get; set; }
        public AuthResponse? AuthData { get; set; }
    }

    public class JwtSettings
    {
        public string Secret { get; set; } = null!;
        public string Issuer { get; set; } = "MarketConnect";
        public string Audience { get; set; } = "MarketConnectClients";
        public int ExpiryMinutes { get; set; } = 60;
    }
}

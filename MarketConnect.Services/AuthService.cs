using MarketConnect.Data;
using MarketConnect.Services.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using Google.Apis.Auth;

namespace MarketConnect.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly JwtSettings _jwt;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext db, JwtSettings jwtSettings, IConfiguration configuration)
        {
            _db = db;
            _jwt = jwtSettings;
            _configuration = configuration;
        }

        // 1. ĐĂNG NHẬP BẰNG GOOGLE
        public async Task<AuthResponse> GoogleLoginAsync(string idToken)
        {
            try
            {
                var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings();
                var clientId = _configuration["Google:ClientId"];
                if (!string.IsNullOrWhiteSpace(clientId))
                {
                    settings.Audience = new[] { clientId };
                }

                var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                var email = payload.Email;
                var name = payload.Name;

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    user = new User
                    {
                        Email = email,
                        Name = name,
                        Phone = null,
                        Role = UserRole.Buyer,
                        // For Google sign-in we don't create a usable local password.
                        // Leave PasswordHash empty to indicate no local password has been set yet.
                        PasswordHash = string.Empty
                    };
                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();
                }

                var token = GenerateJwtToken(user);
                return new AuthResponse
                {
                    Token = token.token,
                    ExpiresAt = token.expiresAt,
                    Email = user.Email,
                    Role = user.Role,
                    FullName = user.Name ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid Google token.", ex);
            }
        }

        // 2. ĐĂNG KÝ BƯỚC 1: KIỂM TRA SỐ ĐIỆN THOẠI & YÊU CẦU OTP
        public async Task RequestRegisterOtpAsync(string phoneNumber, string fullName, string password)
        {
            var phone = (phoneNumber ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(phone)) throw new ArgumentException("Phone number is required.");

            var exists = await _db.Users.AnyAsync(u => u.Phone == phone);
            if (exists) throw new InvalidOperationException("Phone number already registered.");

            ValidatePassword(password);

            var otp = new Random().Next(100000, 999999).ToString();
            var expires = DateTime.UtcNow.AddMinutes(5);

            var verification = new OtpVerification
            {
                PhoneNumber = phone,
                FullName = fullName,
                PasswordHash = HashPassword(password),
                OtpCode = otp,
                ExpiresAt = expires,
                CreatedAt = DateTime.UtcNow
            };

            var existing = await _db.OtpVerifications.FirstOrDefaultAsync(o => o.PhoneNumber == phone);
            if (existing != null)
            {
                _db.OtpVerifications.Remove(existing);
            }

            _db.OtpVerifications.Add(verification);
            await _db.SaveChangesAsync();

            Console.WriteLine($"[OTP] Phone: {phone} Code: {otp} (expires at {expires:O})");
        }

        // 3. ĐĂNG KÝ BƯỚC 2: XÁC THỰC OTP ĐỂ TẠO TÀI KHOẢN
        public async Task<AuthResponse> VerifyOtpAsync(string phoneNumber, string otpCode)
        {
            var phone = (phoneNumber ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(phone)) throw new ArgumentException("Phone number required.");

            var record = await _db.OtpVerifications.FirstOrDefaultAsync(o => o.PhoneNumber == phone);
            if (record == null) throw new InvalidOperationException("No pending verification found.");

            if (record.ExpiresAt < DateTime.UtcNow)
            {
                _db.OtpVerifications.Remove(record);
                await _db.SaveChangesAsync();
                throw new InvalidOperationException("OTP expired.");
            }

            if (record.OtpCode != otpCode) throw new InvalidOperationException("Invalid OTP code.");

            var email = $"{phone}@phone.local";

            var user = new User
            {
                Email = email,
                Name = record.FullName,
                Phone = phone,
                Role = UserRole.Buyer,
                PasswordHash = record.PasswordHash
            };

            _db.Users.Add(user);
            _db.OtpVerifications.Remove(record);
            await _db.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return new AuthResponse
            {
                Token = token.token,
                ExpiresAt = token.expiresAt,
                Email = user.Email,
                Role = user.Role
            };
        }

        public async Task<AuthResponse?> PhoneLoginAsync(PhoneLoginRequest request)
        {
            var phone = (request.PhoneNumber ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(phone)) return null;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == phone);
            if (user == null) return null;
            if (!VerifyPassword(request.Password, user.PasswordHash)) return null;

            var token = GenerateJwtToken(user);
            return new AuthResponse
            {
                Token = token.token,
                ExpiresAt = token.expiresAt,
                Email = user.Email,
                Role = user.Role,
                FullName = user.Name ?? string.Empty
            };
        }

        private static string HashPassword(string password)
        {
            const int iter = 100_000;
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iter,
                HashAlgorithmName.SHA256,
                32
            );

            return $"pbkdf2${iter}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4) return false;
                if (parts[0] != "pbkdf2") return false;

                var iter = int.Parse(parts[1]);
                var salt = Convert.FromBase64String(parts[2]);
                var hash = Convert.FromBase64String(parts[3]);

                var computed = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iter,
                    HashAlgorithmName.SHA256,
                    hash.Length
                );

                return CryptographicOperations.FixedTimeEquals(computed, hash);
            }
            catch
            {
                return false;
            }
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password is required.");

            if (!Regex.IsMatch(password, "[A-Z]"))
                throw new ArgumentException("Password must contain at least one uppercase letter.");

            if (!Regex.IsMatch(password, "[a-z]"))
                throw new ArgumentException("Password must contain at least one lowercase letter.");

            if (!Regex.IsMatch(password, "[0-9]"))
                throw new ArgumentException("Password must contain at least one digit.");

            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
                throw new ArgumentException("Password must contain at least one special character.");
        }

        private (string token, DateTime expiresAt) GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return (tokenString, expires);
        }

        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new InvalidOperationException("Không tìm thấy người dùng.");
            }

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.Name ?? string.Empty,
                Email = user.Email,
                AvatarUrl = null, 
                PhoneNumber = user.Phone
            };
        }
    }
}
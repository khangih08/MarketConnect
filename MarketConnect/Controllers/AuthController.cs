using MarketConnect.Controllers.Dtos;
using MarketConnect.Services;
using MarketConnect.Services.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var res = await _authService.GoogleLoginAsync(dto.IdToken);
                var outDto = new AuthResponseDto
                {
                    UserId = res.UserId,
                    Token = res.Token,
                    Username = res.FullName ?? string.Empty,
                    Email = res.Email,
                    ExpiresAt = res.ExpiresAt
                };
                Response.Cookies.Append("user_id", res.UserId.ToString(), new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
                if (!string.IsNullOrEmpty(outDto.Email)) Response.Cookies.Append("user_email", outDto.Email, new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
                if (!string.IsNullOrEmpty(outDto.Username)) Response.Cookies.Append("user_name", outDto.Username, new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
                return Ok(outDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register-request")]
        public async Task<IActionResult> RegisterRequest([FromBody] RegisterPhoneRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _authService.RequestRegisterOtpAsync(dto.PhoneNumber, dto.FullName, dto.Password);
                return Ok(new { message = "OTP sent (simulated)." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var res = await _authService.VerifyOtpAsync(dto.PhoneNumber, dto.OtpCode);
                var outDto = new AuthResponseDto
                {
                    Token = res.Token,
                    Username = res.FullName ?? string.Empty,
                    Email = res.Email,
                    ExpiresAt = res.ExpiresAt
                };
                return Ok(outDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("check-phone")]
        public async Task<IActionResult> CheckPhone([FromBody] CheckPhoneDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var res = await _authService.CheckPhoneAsync(dto.PhoneNumber);
            return Ok(res);
        }

        [HttpPost("register-phone")]
        public async Task<IActionResult> RegisterPhone([FromBody] PhoneRegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var req = new MarketConnect.Services.Models.PhoneRegisterRequest
                {
                    PhoneNumber = dto.PhoneNumber,
                    FullName = dto.FullName,
                    Password = dto.Password,
                    ConfirmPassword = dto.ConfirmPassword
                };

                var res = await _authService.RegisterPhonePasswordAsync(req);
                var outDto = new AuthResponseDto
                {
                    UserId = res.UserId,
                    Token = res.Token,
                    Username = !string.IsNullOrEmpty(res.FullName) ? res.FullName : res.Email.Split('@')[0],
                    Email = res.Email,
                    ExpiresAt = res.ExpiresAt,
                    FullName = res.FullName
                };
                Response.Cookies.Append("user_id", res.UserId.ToString(), new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
                if (!string.IsNullOrEmpty(outDto.Email)) Response.Cookies.Append("user_email", outDto.Email, new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
                if (!string.IsNullOrEmpty(outDto.Username)) Response.Cookies.Append("user_name", outDto.Username, new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
                return Ok(outDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("phone-login")]
        public async Task<IActionResult> PhoneLogin([FromBody] PhoneLoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var svcReq = new MarketConnect.Services.Models.PhoneLoginRequest
            {
                PhoneNumber = dto.PhoneNumber,
                Password = dto.Password
            };

            var res = await _authService.PhoneLoginDetailedAsync(svcReq);
            if (!res.Success)
            {
                if (res.IsLocked)
                {
                    return StatusCode(423, new { 
                        message = res.Message, 
                        isLocked = true, 
                        remainingMinutes = res.RemainingMinutes 
                    });
                }
                if (res.RequiresRegister)
                {
                    return BadRequest(new { 
                        message = res.Message, 
                        requiresRegister = true 
                    });
                }
                return BadRequest(new { 
                    message = res.Message, 
                    failedCount = res.FailedCount,
                    remainingAttempts = res.FailedCount > 0 ? (5 - res.FailedCount) : 5
                });
            }

            var authData = res.AuthData!;
            var outDto = new AuthResponseDto
            {
                UserId = authData.UserId,
                Token = authData.Token,
                Username = !string.IsNullOrEmpty(authData.FullName) ? authData.FullName : authData.Email.Split('@')[0],
                Email = authData.Email,
                ExpiresAt = authData.ExpiresAt,
                FullName = authData.FullName,
                Role = authData.Role.ToString()
            };

            // Đính kèm Response Cookies để giữ phiên đăng nhập trên toàn bộ giao diện ASP.NET Core
            Response.Cookies.Append("user_id", authData.UserId.ToString(), new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
            Response.Cookies.Append("user_role", authData.Role.ToString(), new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
            if (!string.IsNullOrEmpty(outDto.Email))
            {
                Response.Cookies.Append("user_email", outDto.Email, new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
            }
            if (!string.IsNullOrEmpty(outDto.Username))
            {
                Response.Cookies.Append("user_name", outDto.Username, new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
            }
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                Response.Cookies.Append("user_phone", dto.PhoneNumber, new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddDays(30) });
            }

            return Ok(outDto);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ hoặc không có quyền truy cập." });
                }

                var res = await _authService.GetProfileAsync(userId);
                return Ok(res);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tải thông tin tài khoản." });
            }
        }
    }
}
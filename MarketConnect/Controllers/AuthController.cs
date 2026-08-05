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

        [HttpPost("phone-login")]
        public async Task<IActionResult> PhoneLogin([FromBody] PhoneLoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var svcReq = new MarketConnect.Services.Models.PhoneLoginRequest
            {
                PhoneNumber = dto.PhoneNumber,
                Password = dto.Password
            };

            var res = await _authService.PhoneLoginAsync(svcReq);
            if (res == null)
            {
                return Unauthorized(new { message = "Số điện thoại hoặc mật khẩu không chính xác." });
            }

            var outDto = new AuthResponseDto
            {
                Token = res.Token,
                Username = !string.IsNullOrEmpty(res.FullName) ? res.FullName : res.Email.Split('@')[0],
                Email = res.Email,
                ExpiresAt = res.ExpiresAt
            };
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
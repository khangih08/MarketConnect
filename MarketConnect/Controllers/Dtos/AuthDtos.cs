using System.ComponentModel.DataAnnotations;

namespace MarketConnect.Controllers.Dtos
{

    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string Token { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}

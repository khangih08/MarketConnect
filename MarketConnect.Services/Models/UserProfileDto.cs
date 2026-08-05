namespace MarketConnect.Services.Models
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}

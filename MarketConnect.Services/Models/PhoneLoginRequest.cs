namespace MarketConnect.Services.Models
{
    // Request model for phone-based login inside Services layer
    public class PhoneLoginRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

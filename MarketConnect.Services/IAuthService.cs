using MarketConnect.Services.Models;
using System.Threading.Tasks;


namespace MarketConnect.Services
{
    public interface IAuthService
    {
        // Google IdToken login: returns AuthResponse (creates user if not exists)
        Task<AuthResponse> GoogleLoginAsync(string idToken);

        // Phone registration: request sends PhoneNumber/FullName/Password and receives OTP (saved temporarily)
        Task RequestRegisterOtpAsync(string phoneNumber, string fullName, string password);

        // Verify OTP and finalize registration, returning AuthResponse with JWT
        Task<AuthResponse> VerifyOtpAsync(string phoneNumber, string otpCode);
        Task<AuthResponse?> PhoneLoginAsync(PhoneLoginRequest request);
        Task<PhoneLoginResult> PhoneLoginDetailedAsync(PhoneLoginRequest request);
        Task<PhoneCheckResult> CheckPhoneAsync(string phoneNumber);
        Task<AuthResponse> RegisterPhonePasswordAsync(PhoneRegisterRequest request);
        Task<UserProfileDto> GetProfileAsync(int userId);
    }
}

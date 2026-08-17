using System.Threading.Tasks;
using MarketConnect.Data;

namespace MarketConnect.Services
{
    public interface IAdminMfaService
    {
        Task<string> GenerateMfaSetupSecretAsync(int userId);
        Task<bool> VerifyAndEnrollMfaAsync(int userId, string passcode, string secret);
        Task<bool> ValidateAdminMfaPasscodeAsync(int userId, string passcode);
        bool IsMfaRequiredForRole(UserRole role);
        string EncryptSecret(string plainSecret);
        string DecryptSecret(string encryptedSecret);
    }
}

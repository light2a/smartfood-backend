using DAL.Models;
using System;
using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(string email, string password);
        Task<Account> RegisterAsync(string fullName, string email, string password, string phoneNumber);
        Task<Account> RegisterSellerAsync(string fullName, string email, string password, string phoneNumber);
        Task<string> GenerateJwtTokenAsync(Account account);
        Task<Account> HandleExternalLoginAsync(string email, string fullName, string externalProvider, string externalProviderKey);

        // ✅ OTP Sign-In
        Task<bool> SaveOtpAsync(string email, string otp, DateTime expiration);
        Task<bool> VerifyOtpAsync(string email, string otp);

        // ✅ Forgot & Reset Password
        Task<Account?> GetAccountByEmailAsync(string email);
        Task SavePasswordResetTokenAsync(int accountId, string token, DateTime expiration);
        Task<bool> VerifyPasswordResetTokenAsync(int accountId, string token);
        Task InvalidatePasswordResetTokenAsync(int accountId, string token);
        Task UpdateAccountAsync(Account account);
        Task InvalidateOtpAsync(string email);
        Task<OtpInfo> GetCurrentOtpAsync(string email);
        Task<Account> UpdateAccountAsync(int accountId, string fullName, string phoneNumber);
        Task<Account> DeactivateAccountAsync(int accountId);
    }
}

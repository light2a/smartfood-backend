using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ISellerRepository _sellerRepository;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;

        public AuthService(IAccountRepository accountRepository, ISellerRepository sellerRepository, IConfiguration configuration, IDistributedCache cache)
        {
            _accountRepository = accountRepository;
            _sellerRepository = sellerRepository;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            var account = await _accountRepository.GetByEmailAsync(email);
            
            if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.Password))
                return null;

            if (account.IsActive == false)
            {
                throw new Exception("Tài khoản của bạn đã bị cấm.");
            }
            return await GenerateJwtTokenAsync(account);
        }

        public async Task<Account> RegisterAsync(string fullName, string email, string password, string phoneNumber)
        {
            var existing = await _accountRepository.GetByEmailAsync(email);
            if (existing != null)
                throw new Exception("Email already registered.");

            var account = new Account
            {
                FullName = fullName,
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                RoleId = 1,
                PhoneNumber = phoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };

            return await _accountRepository.AddAsync(account);
        }

        public async Task<Account> RegisterSellerAsync(string fullName, string email, string password, string phoneNumber)
        {
            var existing = await _accountRepository.GetByEmailAsync(email);
            if (existing != null)
                throw new Exception("Email already registered.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var account = new Account
            {
                FullName = fullName,
                Email = email,
                Password = hashedPassword,
                IsActive = false,
                RoleId = 2,
                PhoneNumber = phoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };

            var createdAccount = await _accountRepository.AddAsync(account);

            var seller = new Seller
            {
                UserAccountId = createdAccount.AccountId,
                DisplayName = createdAccount.FullName,
                Description = null,
                Status = SellerStatus.Unavailable
            };

            await _sellerRepository.AddAsync(seller);

            return createdAccount;
        }

        // ✅ OTP Save
        public async Task<bool> SaveOtpAsync(string email, string otp, DateTime expiration)
        {
            var otpInfo = new { Code = otp, Expiration = expiration };
            var otpJson = JsonSerializer.Serialize(otpInfo);

            try
            {
                await _cache.SetStringAsync($"OTP_{email}", otpJson, new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = expiration
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ✅ OTP Verify
        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            var cached = await _cache.GetStringAsync($"OTP_{email}");
            if (cached == null) return false;

            var otpInfo = JsonSerializer.Deserialize<OtpInfo>(cached);
            if (otpInfo == null || otpInfo.Expiration < DateTime.UtcNow || otpInfo.Code != otp)
                return false;

            await _cache.RemoveAsync($"OTP_{email}");
            return true;
        }

        // ✅ Forgot/Reset Password
        public async Task<Account?> GetAccountByEmailAsync(string email)
            => await _accountRepository.GetByEmailAsync(email);

        public async Task SavePasswordResetTokenAsync(int accountId, string token, DateTime expiration)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null) throw new Exception("Account not found.");

            account.ResetToken = token;
            account.ResetTokenExpires = expiration;
            await _accountRepository.UpdateAsync(account);
        }

        public async Task<bool> VerifyPasswordResetTokenAsync(int accountId, string token)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            return account != null &&
                   account.ResetToken == token &&
                   account.ResetTokenExpires.HasValue &&
                   account.ResetTokenExpires.Value > DateTime.UtcNow;
        }

        public async Task InvalidatePasswordResetTokenAsync(int accountId, string token)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null) return;

            account.ResetToken = null;
            account.ResetTokenExpires = null;
            await _accountRepository.UpdateAsync(account);
        }

        public async Task UpdateAccountAsync(Account account)
            => await _accountRepository.UpdateAsync(account);

        public async Task<Account> HandleExternalLoginAsync(string email, string fullName, string provider, string providerKey)
        {
            var existing = await _accountRepository.GetByEmailAsync(email);
            if (existing != null)
            {
                existing.FullName = fullName ?? existing.FullName;
                existing.ExternalProvider = provider;
                existing.ExternalProviderKey = providerKey;
                existing.UpdateAt = DateTime.UtcNow;
                return await _accountRepository.UpdateAsync(existing);
            }

            var newAccount = new Account
            {
                Email = email,
                FullName = fullName,
                Password = string.Empty,
                IsActive = false,
                RoleId = 1,
                ExternalProvider = provider,
                ExternalProviderKey = providerKey,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };

            return await _accountRepository.AddAsync(newAccount);
        }

        public async Task<string> GenerateJwtTokenAsync(Account account)
        {
            int? sellerId = null;
            if (account.Role?.RoleName?.Equals("Seller", StringComparison.OrdinalIgnoreCase) == true)
            {
                var seller = await _sellerRepository.GetByUserAccountIdAsync(account.AccountId);
                sellerId = seller?.Id;
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.AccountId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email),
                new Claim(ClaimTypes.Role, account.Role?.RoleName ?? "Customer")
            };

            if (sellerId.HasValue)
                claims.Add(new Claim("SellerId", sellerId.Value.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task InvalidateOtpAsync(string email)
        {
            await _cache.RemoveAsync(GetOtpCacheKey(email));
        }

        private string GetOtpCacheKey(string email)
        {
            return $"OTP_{email}";
        }
        public async Task<OtpInfo> GetCurrentOtpAsync(string email)
        {
            var otpJson = await _cache.GetStringAsync(GetOtpCacheKey(email));
            if (string.IsNullOrEmpty(otpJson))
                return null;
            return JsonSerializer.Deserialize<OtpInfo>(otpJson);
        }

        public async Task<Account> UpdateAccountAsync(int accountId, string fullName, string phoneNumber)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
                throw new Exception("Account not found.");

            account.FullName = fullName;
            account.PhoneNumber = phoneNumber;
            account.UpdateAt = DateTime.UtcNow;

            return await _accountRepository.UpdateAsync(account);
        }

        public async Task<Account> DeactivateAccountAsync(int accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
                throw new Exception("Account not found.");

            account.IsActive = false;
            account.UpdateAt = DateTime.UtcNow;

            return await _accountRepository.UpdateAsync(account);
        }

        public async Task BanAccountAsync(int accountId, bool isBanned)
        {
            await _accountRepository.BanAccountAsync(accountId, isBanned);
        }
        public IQueryable<Account> GetAll()
        {
            return _accountRepository.GetAll();
        }
    }
}

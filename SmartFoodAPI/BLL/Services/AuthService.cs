using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ISellerRepository _sellerRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IAccountRepository accountRepository, ISellerRepository sellerRepository, IConfiguration configuration)
        {
            _accountRepository = accountRepository;
            _sellerRepository = sellerRepository;
            _configuration = configuration;
        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            var account = await _accountRepository.GetByEmailAsync(email);
            if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.Password) || account.IsActive == false)
                return null;

            // Now generate JWT with SellerId if applicable
            return await GenerateJwtTokenAsync(account);
        }

        public async Task<Account> RegisterAsync(string fullName, string email, string password, string phonenumber)
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
                IsActive = true,
                RoleId = 1, // Default = Customer
                PhoneNumber = phonenumber
            };

            return await _accountRepository.AddAsync(account);
        }

        public async Task<Account> RegisterSellerAsync(string fullName, string email, string password, string phonenumber)
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
                IsActive = false, // inactive until admin approves
                RoleId = 2,       // Seller role
                PhoneNumber = phonenumber
            };

            var createdAccount = await _accountRepository.AddAsync(account);

            // Create corresponding seller entry
            var seller = new Seller
            {
                UserAccountId = createdAccount.AccountId,
                DisplayName = createdAccount.FullName,
                Description = null
            };

            await _sellerRepository.AddAsync(seller);

            return createdAccount;
        }
        public async Task<Account> HandleExternalLoginAsync(string email, string fullName, string externalProvider, string externalProviderKey)
        {
            try
            {
                var existing = await _accountRepository.GetByEmailAsync(email);

                if (existing != null)
                {
                    // Update existing account with Google info
                    existing.FullName = fullName ?? existing.FullName;
                    existing.ExternalProvider = externalProvider;
                    existing.ExternalProviderKey = externalProviderKey;
                    existing.UpdateAt = DateTime.UtcNow;

                    var updated = await _accountRepository.UpdateAsync(existing);
                    Console.WriteLine($"[GoogleOAuth] Updated existing user: {updated.Email} (ID: {updated.AccountId})");
                    return updated;
                }

                // Create new account
                var newAccount = new Account
                {
                    Email = email,
                    FullName = fullName,
                    Password = string.Empty, // Google login doesn't need a password
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                    RoleId = 1, // Default = Customer
                    ExternalProvider = externalProvider,
                    ExternalProviderKey = externalProviderKey
                };

                var created = await _accountRepository.AddAsync(newAccount);
                Console.WriteLine($"[GoogleOAuth] Created new user: {created.Email} (ID: {created.AccountId})");

                return created;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleOAuth] Error saving external user: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates JWT token, including SellerId if user is a seller.
        /// </summary>
        public async Task<string> GenerateJwtTokenAsync(Account account)
        {
            int? sellerId = null;

            // Only lookup seller if role is "Seller"
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
            {
                claims.Add(new Claim("SellerId", sellerId.Value.ToString()));
            }

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
    }
}

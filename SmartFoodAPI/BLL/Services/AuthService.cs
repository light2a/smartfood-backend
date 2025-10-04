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
        private readonly IConfiguration _configuration;
        private readonly ISellerRepository _sellerRepository;

        public AuthService(IAccountRepository accountRepository, ISellerRepository sellerRepository, IConfiguration configuration)
        {
            _accountRepository = accountRepository;
            _sellerRepository = sellerRepository; // assign it
            _configuration = configuration;
        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            var account = await _accountRepository.GetByEmailAsync(email);
            if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.Password) || account.IsActive == false)
                return null;

            return GenerateJwtToken(account);
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

        private string GenerateJwtToken(Account account)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.AccountId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email),
                new Claim(ClaimTypes.Role, account.Role?.RoleName ?? "Customer")
            };

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

            // Create seller entry with minimal info
            var seller = new Seller
            {
                UserAccountId = createdAccount.AccountId,
                DisplayName = account.FullName,
                Description = null // to be filled later via UpdateSeller API
            };

            // Save seller in database (assuming you have ISellerRepository)
            await _sellerRepository.AddAsync(seller);

            return createdAccount;
        }

    }
}

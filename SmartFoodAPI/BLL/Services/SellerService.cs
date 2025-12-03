using BLL.DTOs.Seller;
using BLL.Extensions;
using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class SellerService : ISellerService
    {
        private readonly ISellerRepository _sellerRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<SellerService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public SellerService(ISellerRepository sellerRepository, IAccountRepository accountRepository, ILogger<SellerService> logger, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _sellerRepository = sellerRepository;
            _accountRepository = accountRepository;
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<Seller>> GetAllSellersAsync()
        {
            return await _sellerRepository.GetAllAsync();
        }

        public async Task<Seller?> GetSellerByIdAsync(int id)
        {
            return await _sellerRepository.GetByIdAsync(id);
        }

        public async Task ApproveSellerAsync(int sellerId)
        {
            var seller = await _sellerRepository.GetByIdAsync(sellerId);
            if (seller == null)
                throw new Exception("Seller not found.");

            // ✅ Approve seller without Stripe
            seller.Status = SellerStatus.Available;
            await _sellerRepository.UpdateAsync(seller);

            // ✅ Activate linked user account
            var userAccount = await _accountRepository.GetByIdAsync(seller.UserAccountId);
            if (userAccount != null)
            {
                userAccount.IsActive = true;
                await _accountRepository.UpdateAsync(userAccount);
            }
        }

        public async Task ValidateSellerBankAsync(int sellerId)
        {
            var seller = await _sellerRepository.GetByIdAsync(sellerId);
            if (seller == null) throw new Exception("Seller not found.");
            if (string.IsNullOrEmpty(seller.BankAccountNumber) || !seller.BankCode.HasValue)
                throw new Exception("Seller bank info is required for payouts.");
        }

        public async Task UpdateBankInfoAsync(int sellerId, DTOs.Seller.UpdateSellerBankInfoRequestDto dto)
        {
            var seller = await _sellerRepository.GetByIdAsync(sellerId);
            if (seller == null)
                throw new Exception("Seller not found.");

            seller.BankAccountNumber = dto.BankAccountNumber;
            seller.BankCode = dto.BankCode;

            await _sellerRepository.UpdateAsync(seller);
        }

        public async Task<DTOs.Seller.SellerInfoRequestDto> GetSellerInfoAsync(int sellerId)
        {
            var seller = await _sellerRepository.GetByIdAsync(sellerId);
            if (seller == null)
                throw new Exception("Seller not found.");

            return new DTOs.Seller.SellerInfoRequestDto
            {
                DisplayName = seller.DisplayName,
                Description = seller.Description,
            };
        }

        public async Task UpdateSellerInfoAsync(int sellerId, DTOs.Seller.UpdateSellerInfoRequestDto dto)
        {
            var seller = await _sellerRepository.GetByIdAsync(sellerId);
            if (seller == null)
                throw new Exception("Seller not found.");

            seller.DisplayName = dto.DisplayName;
            seller.Description = dto.Description;

            await _sellerRepository.UpdateAsync(seller);
        }

        public async Task<SellerBankInfoResponse> GetBankInfoAsync(int sellerId)
        {
            var seller = await _sellerRepository.GetByIdAsync(sellerId);
            if (seller == null)
                throw new Exception("Seller not found.");

            return new SellerBankInfoResponse
            {
                BankAccountNumber = seller.BankAccountNumber,
                BankCode = seller.BankCode,
                BankName = seller.BankCode?.GetDescription()
            };
        }
    }
}

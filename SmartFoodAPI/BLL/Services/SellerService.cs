using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using Stripe;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class SellerService : ISellerService
    {
        private readonly ISellerRepository _sellerRepository;
        private readonly IAccountRepository _accountRepository;

        public SellerService(ISellerRepository sellerRepository, IAccountRepository accountRepository)
        {
            _sellerRepository = sellerRepository;
            _accountRepository = accountRepository;
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

            // ✅ Create Stripe connected account
            var options = new AccountCreateOptions
            {
                Type = "express",
                Email = seller.User.Email,
                BusinessType = "individual",
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                }
            };

            var service = new AccountService();
            var account = service.Create(options);

            // ✅ Save Stripe account ID
            seller.StripeAccountId = account.Id;
            seller.Status = SellerStatus.Available;
            await _sellerRepository.UpdateAsync(seller);

            // ✅ Activate account
            var userAccount = await _accountRepository.GetByIdAsync(seller.UserAccountId);
            if (userAccount != null)
            {
                userAccount.IsActive = true;
                await _accountRepository.UpdateAsync(userAccount);
            }
        }

        public async Task<string> GenerateStripeOnboardingLinkAsync(int sellerId)
        {
            var seller = await _sellerRepository.GetByIdAsync(sellerId);
            if (seller == null || string.IsNullOrEmpty(seller.StripeAccountId))
                throw new Exception("Seller or Stripe account not found.");

            var accountLinkService = new AccountLinkService();
            var accountLink = accountLinkService.Create(new AccountLinkCreateOptions
            {
                Account = seller.StripeAccountId,
                RefreshUrl = "https://your-frontend.com/seller/stripe/refresh",
                ReturnUrl = "https://your-frontend.com/seller/stripe/success",
                Type = "account_onboarding",
            });

            return accountLink.Url;
        }
        public async Task MarkStripeOnboardingCompletedAsync(string stripeAccountId)
        {
            var seller = await _sellerRepository.GetByStripeAccountIdAsync(stripeAccountId);
            if (seller == null)
                throw new Exception("Seller not found for given Stripe account ID.");

            seller.IsStripeOnboardingCompleted = true;
            seller.Status = SellerStatus.Available;
            await _sellerRepository.UpdateAsync(seller);
        }


    }
}

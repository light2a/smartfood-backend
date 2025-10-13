using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
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

            seller.Status = SellerStatus.Available;

            var account = await _accountRepository.GetByIdAsync(seller.UserAccountId);
            if (account != null)
            {
                account.IsActive = true;
                await _accountRepository.UpdateAsync(account);
            }

            await _sellerRepository.UpdateAsync(seller);
        }
    }
}

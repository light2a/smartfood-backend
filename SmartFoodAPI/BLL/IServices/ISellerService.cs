using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface ISellerService
    {
        Task<IEnumerable<Seller>> GetAllSellersAsync();
        Task<Seller?> GetSellerByIdAsync(int id);
        Task ApproveSellerAsync(int sellerId);
        Task<string> GenerateStripeOnboardingLinkAsync(int sellerId);
        Task MarkStripeOnboardingCompletedAsync(string stripeAccountId);
    }
}

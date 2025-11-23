using DAL.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DAL.IRepositories
{
    public interface ISellerRepository
    {
        Task<Seller> AddAsync(Seller seller);
        Task<Seller?> GetByIdAsync(int id);
        Task<IEnumerable<Seller>> GetAllAsync();
        Task UpdateAsync(Seller seller);
        Task DeleteAsync(int id);
        Task<Seller?> GetByUserAccountIdAsync(int accountId);
        Task ApproveSellerAsync(int sellerId);
        Task<Seller?> GetByOrderIdAsync(int orderId);
    }
}
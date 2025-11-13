using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IOrderRepository
    {
        Task<PagedResult<Order>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<Order> AddAsync(Order order);
        Task<Order?> GetByIdAsync(int id);
        Task<Order?> GetDetailByIdAsync(int id);
        Task UpdateAsync(Order order);
        Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerAccountId);
        Task<List<Order>> GetAllOrdersWithItemsAsync();

        Task<PagedResult<Order>> GetPagedBySellerAsync(int sellerId, int pageNumber, int pageSize, string? keyword);

    }
}

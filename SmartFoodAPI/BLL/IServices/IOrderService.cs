using BLL.DTOs.Order;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface IOrderService
    {
        Task<PagedResult<OrderDetailDto>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<CreateOrderResponse> CreateOrderAsync(int customerAccountId, CreateOrderRequest request);
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, string? note = null);
        Task<OrderDetailDto> GetOrderDetailAsync(int customerAccountId, int orderId);
        Task<IEnumerable<OrderSummaryDto>> GetOrdersByCustomerAsync(int customerAccountId);
        Task<bool> CancelOrderAsync(int customerAccountId, int orderId);
        Task<IEnumerable<CategoryPopularityDto>> GetCategoryPopularityAsync(DateTime? from = null, DateTime? to = null);

    }
}

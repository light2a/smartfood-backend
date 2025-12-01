using BLL.DTOs.Order;
using BLL.DTOs.Seller;
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
        IQueryable<Order> GetAll();
        Task<PagedResult<OrderDetailDto>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<CreateOrderResponse> CreateOrderAsync(int customerAccountId, CreateOrderRequest request);
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, string? note = null);
        Task<OrderDetailDto> GetOrderDetailAsync(int customerAccountId, int orderId);
        Task<IEnumerable<OrderSummaryDto>> GetOrdersByCustomerAsync(int customerAccountId);
        Task<bool> CancelOrderAsync(int customerAccountId, int orderId);
        Task<IEnumerable<CategoryPopularityDto>> GetCategoryPopularityAsync(DateTime? from = null, DateTime? to = null);

        Task<PagedResult<OrderDetailDto>> GetPagedBySellerAsync(int sellerId, int pageNumber, int pageSize, string? keyword);
        Task<OrderDetailDto> GetOrderDetailBySellerAsync(int sellerId, int orderId);
        Task<bool> UpdateOrderStatusBySellerAsync(int sellerId, int orderId, string newStatus, string? note);
        Task<decimal> CalculateShippingFeeAsync(int restaurantId, double latitude, double longitude);
        Task<decimal> GetSellerRevenueAsync(int sellerId);
        Task<IEnumerable<RevenueOverTimeDto>> GetSellerRevenueOverTimeAsync(int sellerId, string period);
        Task<IEnumerable<PopularMenuItemDto>> GetSellerPopularMenuItemsAsync(int sellerId);
        Task<IEnumerable<OrderStatusDistributionDto>> GetSellerOrderStatusDistributionAsync(int sellerId);
        Task<SellerStatsDto> GetSellerDashboardStatisticsAsync(int sellerId);
    }
}
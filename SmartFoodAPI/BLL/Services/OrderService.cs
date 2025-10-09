using BLL.DTOs.Order;
using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMenuItemRepository _menuItemRepository;

        public OrderService(IOrderRepository orderRepository, IMenuItemRepository menuItemRepository)
        {
            _orderRepository = orderRepository;
            _menuItemRepository = menuItemRepository;
        }

        public async Task<CreateOrderResponse> CreateOrderAsync(int customerAccountId, CreateOrderRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
                throw new Exception("Order must contain at least one item.");

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;
            int restaurantId = 0;

            foreach (var item in request.Items)
            {
                var menuItem = await _menuItemRepository.GetByIdAsync(item.MenuItemId);
                if (menuItem == null)
                    throw new Exception($"Menu item ID {item.MenuItemId} is not available.");

                if (restaurantId == 0)
                    restaurantId = menuItem.RestaurantId;
                else if (restaurantId != menuItem.RestaurantId)
                    throw new Exception("All menu items must be from the same restaurant.");

                var orderItem = new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    Qty = item.Quantity,
                    UnitPrice = menuItem.Price
                };

                totalAmount += menuItem.Price * item.Quantity;
                orderItems.Add(orderItem);
            }

            // Create initial order with "Created" status
            var order = new Order
            {
                CustomerAccountId = customerAccountId,
                RestaurantId = restaurantId,
                ShippingFee = 0,
                CommissionPercent = 0,
                TotalAmount = totalAmount,
                FinalAmount = totalAmount,
                OrderItems = orderItems,
                StatusHistory = new List<OrderStatusHistory>
                {
                    new OrderStatusHistory
                    {
                        Status = "Created",
                        Note = "Order created successfully."
                    }
                }
            };

            var createdOrder = await _orderRepository.AddAsync(order);

            return new CreateOrderResponse
            {
                OrderId = createdOrder.Id,
                TotalAmount = totalAmount,
                FinalAmount = totalAmount,
                Message = "Order created with status: Created."
            };
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, string? note = null)
        {
            var allowedStatuses = new[] { "Created", "Preparing", "Shipping", "Completed", "Cancelled" };
            if (!allowedStatuses.Contains(newStatus))
                throw new Exception("Invalid status value.");

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            var statusHistory = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = newStatus,
                Note = note ?? $"Order status updated to {newStatus}."
            };

            order.StatusHistory.Add(statusHistory);
            await _orderRepository.UpdateAsync(order);
            return true;
        }
        public async Task<OrderDetailDto> GetOrderDetailAsync(int customerAccountId, int orderId)
        {
            var order = await _orderRepository.GetDetailByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            if (order.CustomerAccountId != customerAccountId)
                throw new Exception("You are not authorized to view this order.");

            return new OrderDetailDto
            {
                OrderId = order.Id,
                TotalAmount = order.TotalAmount,
                ShippingFee = order.ShippingFee,
                FinalAmount = order.FinalAmount,
                CreatedAt = order.CreatedAt,
                RestaurantName = order.Restaurant?.Name ?? "Unknown Restaurant",
                RestaurantAddress = order.Restaurant?.Address,

                Items = order.OrderItems.Select(oi => new OrderItemDetailDto
                {
                    MenuItemName = oi.MenuItem.Name,
                    Quantity = oi.Qty,
                    UnitPrice = oi.UnitPrice
                }).ToList(),

                StatusHistory = order.StatusHistory
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new OrderStatusHistoryDto
                    {
                        Status = s.Status,
                        Note = s.Note,
                        CreatedAt = s.CreatedAt
                    }).ToList()
            };
        }
        public async Task<IEnumerable<OrderSummaryDto>> GetOrdersByCustomerAsync(int customerAccountId)
        {
            var orders = await _orderRepository.GetOrdersByCustomerIdAsync(customerAccountId);

            return orders.Select(o => new OrderSummaryDto
            {
                OrderId = o.Id,
                RestaurantName = o.Restaurant?.Name ?? "Unknown Restaurant",
                TotalAmount = o.TotalAmount,
                FinalAmount = o.FinalAmount,
                CreatedAt = o.CreatedAt,
                LatestStatus = o.StatusHistory
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefault()?.Status ?? "Created"
            }).ToList();
        }

        public async Task<bool> CancelOrderAsync(int customerAccountId, int orderId)
        {
            var order = await _orderRepository.GetDetailByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            // Verify customer owns the order
            if (order.CustomerAccountId != customerAccountId)
                throw new Exception("You are not authorized to cancel this order.");

            // Check latest status
            var latestStatus = order.StatusHistory
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault()?.Status;

            if (latestStatus != "Created")
                throw new Exception("Only orders in 'Created' status can be cancelled.");

            // Add new status history
            var cancelStatus = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "Cancelled",
                Note = "Order cancelled by customer."
            };

            order.StatusHistory.Add(cancelStatus);
            await _orderRepository.UpdateAsync(order);

            return true;
        }

    }
}

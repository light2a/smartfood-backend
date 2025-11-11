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

        public async Task<PagedResult<OrderDetailDto>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var pagedResult = await _orderRepository.GetPagedAsync(pageNumber, pageSize, keyword);
            return new PagedResult<OrderDetailDto>
            {
                Items = pagedResult.Items.Select(o => new OrderDetailDto
                {
                    OrderId = o.Id,
                    TotalAmount = o.TotalAmount,
                    ShippingFee = o.ShippingFee,
                    FinalAmount = o.FinalAmount,
                    CreatedAt = o.CreatedAt,
                    RestaurantName = o.Restaurant?.Name ?? "Unknown Restaurant",
                    Items = o.OrderItems.Select(oi => new OrderItemDetailDto
                    {
                        MenuItemName = oi.MenuItem.Name,
                        Quantity = oi.Qty,
                        UnitPrice = oi.UnitPrice
                    }).ToList(),
                    StatusHistory = o.StatusHistory
                        .OrderByDescending(s => s.CreatedAt)
                        .Select(s => new OrderStatusHistoryDto
                        {
                            Status = s.Status,
                            Note = s.Note,
                            CreatedAt = s.CreatedAt
                        }).ToList()
                }),
                TotalItems = pagedResult.TotalItems,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };
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

            // ✅ Determine shipping fee based on enum value
            decimal shippingFee = 0;
            if (request.OrderType == OrderType.Delivery)
            {
                // Later: replace with distance-based calculation
                shippingFee = 15000m;
            }

            // ✅ Create order with enum OrderType
            var order = new Order
            {
                CustomerAccountId = customerAccountId,
                RestaurantId = restaurantId,
                TotalAmount = totalAmount,
                ShippingFee = shippingFee,
                CommissionPercent = 0,
                FinalAmount = totalAmount + shippingFee,
                OrderType = request.OrderType,
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
                FinalAmount = order.FinalAmount,
                Message = $"Order created with type '{order.OrderType}' and status 'Created'."
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
        public async Task<IEnumerable<CategoryPopularityDto>> GetCategoryPopularityAsync(DateTime? from = null, DateTime? to = null)
        {
            var orders = await _orderRepository.GetAllOrdersWithItemsAsync(); // include items + category

            // Filter by date range
            if (from.HasValue)
                orders = orders.Where(o => o.CreatedAt.Date >= from.Value.Date).ToList();
            if (to.HasValue)
                orders = orders.Where(o => o.CreatedAt.Date <= to.Value.Date).ToList();

            // Flatten all order items with categories
            var allItems = orders
                .SelectMany(o => o.OrderItems) // OrderItems includes MenuItem -> Category
                .Where(oi => oi.MenuItem != null && oi.MenuItem.Category != null);

            // Group by category
            var grouped = allItems
                .GroupBy(oi => oi.MenuItem.Category.Name)
                .Select(g => new CategoryPopularityDto
                {
                    Category = g.Key,
                    OrdersCount = g.Count(),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Qty) // use correct properties
                })
                .OrderByDescending(g => g.OrdersCount)
                .ToList();


            return grouped;
        }

    }
}

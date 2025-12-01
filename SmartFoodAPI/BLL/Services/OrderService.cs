using BLL.DTOs.Order;
using BLL.DTOs.Seller;
using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query;

namespace BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMenuItemRepository _menuItemRepository;
        private readonly IPaymentService _paymentService;
        private readonly IRestaurantRepository _restaurantRepository;
        private readonly ILogger<OrderService> _logger;

        public IQueryable<Order> GetAll()
        {
            return _orderRepository.GetAll();
        }

        public OrderService(IOrderRepository orderRepository, IMenuItemRepository menuItemRepository, IPaymentService paymentService, IRestaurantRepository restaurantRepository, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _menuItemRepository = menuItemRepository;
            _paymentService = paymentService;
            _restaurantRepository = restaurantRepository;
            _logger = logger;
        }

        public async Task<PagedResult<OrderDetailDto>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var pagedResult = await _orderRepository.GetPagedAsync(pageNumber, pageSize, keyword);
            return new PagedResult<OrderDetailDto>
            {
                Items = pagedResult.Items.Select(o => new OrderDetailDto
                {
                    OrderId = o.Id,
                    CustomerAccountId = o.CustomerAccountId,
                    TotalAmount = o.TotalAmount,
                    CommissionPercent = o.CommissionPercent,
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
            _logger.LogInformation("Attempting to create order for customer {CustomerId} with request: {Request}", customerAccountId, JsonSerializer.Serialize(request));

            if (request.Items == null || request.Items.Count == 0)
            {
                _logger.LogError("CreateOrder failed: Order must contain at least one item.");
                throw new Exception("Order must contain at least one item.");
            }

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;
            int restaurantId = 0;

            foreach (var item in request.Items)
            {
                _logger.LogInformation("Processing menu item ID {MenuItemId} with quantity {Quantity}", item.MenuItemId, item.Quantity);
                var menuItem = await _menuItemRepository.GetByIdAsync(item.MenuItemId);
                if (menuItem == null)
                {
                    _logger.LogError("CreateOrder failed: Menu item ID {MenuItemId} is not available.", item.MenuItemId);
                    throw new Exception($"Menu item ID {item.MenuItemId} is not available.");
                }

                if (restaurantId == 0)
                {
                    restaurantId = menuItem.RestaurantId;
                    _logger.LogInformation("Set restaurant ID to {RestaurantId} based on first item.", restaurantId);
                }
                else if (restaurantId != menuItem.RestaurantId)
                {
                    _logger.LogError("CreateOrder failed: All menu items must be from the same restaurant. Expected {ExpectedRestaurantId}, got {ActualRestaurantId}", restaurantId, menuItem.RestaurantId);
                    throw new Exception("All menu items must be from the same restaurant.");
                }

                orderItems.Add(new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    Qty = item.Quantity,
                    UnitPrice = menuItem.Price
                });

                totalAmount += menuItem.Price * item.Quantity;
            }

            decimal shippingFee = 0;
            if (request.OrderType == OrderType.Delivery)
            {
                if (request.DeliveryLatitude == null || request.DeliveryLongitude == null)
                {
                    throw new Exception("Delivery address is required for delivery orders.");
                }
                shippingFee = await CalculateShippingFeeAsync(restaurantId, request.DeliveryLatitude.Value, request.DeliveryLongitude.Value);
            }

            _logger.LogInformation("Calculated total amount: {TotalAmount}, Shipping fee: {ShippingFee}", totalAmount, shippingFee);

            var order = new Order
            {
                CustomerAccountId = customerAccountId,
                RestaurantId = restaurantId,
                TotalAmount = totalAmount,
                ShippingFee = shippingFee,
                CommissionPercent = 0,
                FinalAmount = totalAmount + shippingFee,
                OrderType = request.OrderType,
                DeliveryAddress = request.DeliveryAddress,
                DeliveryLatitude = request.DeliveryLatitude,
                DeliveryLongitude = request.DeliveryLongitude,
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

            _logger.LogInformation("Adding order to the database...");
            var createdOrder = await _orderRepository.AddAsync(order);
            _logger.LogInformation("Order {OrderId} created successfully in database.", createdOrder.Id);

            _logger.LogInformation("Generating PayOS payment URL for order {OrderId}...", createdOrder.Id);
            string paymentUrl = await _paymentService.CreatePayOsOrderAsync(createdOrder.Id);
            _logger.LogInformation("Payment URL generated for order {OrderId}.", createdOrder.Id);

            var response = new CreateOrderResponse
            {
                OrderId = createdOrder.Id,
                TotalAmount = totalAmount,
                FinalAmount = order.FinalAmount,
                PaymentUrl = paymentUrl,
                Message = $"Order created with type '{order.OrderType}' and status 'Created'."
            };

            _logger.LogInformation("Order creation process completed successfully for order {OrderId}.", createdOrder.Id);
            return response;
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
                CustomerAccountId = order.CustomerAccountId,
                TotalAmount = order.TotalAmount,
                CommissionPercent = order.CommissionPercent,
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
        public async Task<PagedResult<OrderDetailDto>> GetPagedBySellerAsync(int sellerId, int pageNumber, int pageSize, string? keyword)
        {
            var pagedOrders = await _orderRepository.GetPagedBySellerAsync(sellerId, pageNumber, pageSize, keyword);

            return new PagedResult<OrderDetailDto>
            {
                Items = pagedOrders.Items.Select(o => new OrderDetailDto
                {
                    OrderId = o.Id,
                    CustomerAccountId = o.CustomerAccountId,
                    TotalAmount = o.TotalAmount,
                    CommissionPercent = o.CommissionPercent,
                    FinalAmount = o.FinalAmount,
                    CreatedAt = o.CreatedAt,
                    RestaurantName = o.Restaurant?.Name,
                    DeliveryAddress = o.DeliveryAddress,
                    Items = o.OrderItems.Select(i => new OrderItemDetailDto
                    {
                        MenuItemName = i.MenuItem.Name,
                        Quantity = i.Qty,
                        UnitPrice = i.UnitPrice
                    }).ToList(),
                    StatusHistory = o.StatusHistory
                        .OrderByDescending(s => s.CreatedAt)
                        .Select(s => new OrderStatusHistoryDto
                        {
                            Status = s.Status,
                            Note = s.Note,
                            CreatedAt = s.CreatedAt
                        }).ToList()
                }).ToList(),
                TotalItems = pagedOrders.TotalItems,
                PageNumber = pagedOrders.PageNumber,
                PageSize = pagedOrders.PageSize
            };
        }

        public async Task<OrderDetailDto> GetOrderDetailBySellerAsync(int sellerId, int orderId)
        {
            var order = await _orderRepository.GetDetailByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            if (order.Restaurant?.SellerId != sellerId)
                throw new Exception("Unauthorized: Order does not belong to your restaurant.");

            return new OrderDetailDto
            {
                OrderId = order.Id,
                CustomerAccountId = order.CustomerAccountId,
                TotalAmount = order.TotalAmount,
                CommissionPercent = order.CommissionPercent,
                FinalAmount = order.FinalAmount,
                CreatedAt = order.CreatedAt,
                RestaurantName = order.Restaurant?.Name ?? "Unknown Restaurant",
                Items = order.OrderItems.Select(i => new OrderItemDetailDto
                {
                    MenuItemName = i.MenuItem.Name,
                    Quantity = i.Qty,
                    UnitPrice = i.UnitPrice
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

        public async Task<bool> UpdateOrderStatusBySellerAsync(int sellerId, int orderId, string newStatus, string? note)
        {
            var order = await _orderRepository.GetDetailByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            if (order.Restaurant?.SellerId != sellerId)
                throw new Exception("Unauthorized: Order does not belong to your restaurant.");

            var allowedStatuses = new[] { "Created", "Preparing", "Shipping", "Completed" };
            if (!allowedStatuses.Contains(newStatus))
                throw new Exception("Invalid status transition.");

            var latest = order.StatusHistory.OrderByDescending(s => s.CreatedAt).FirstOrDefault()?.Status;
            if (latest == "Completed" || latest == "Cancelled")
                throw new Exception("Cannot change status of a completed/cancelled order.");

            var newHistory = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = newStatus,
                Note = note ?? $"Order status updated to {newStatus} by seller."
            };

            order.StatusHistory.Add(newHistory);
            await _orderRepository.UpdateAsync(order);
            return true;
        }

        public async Task<decimal> CalculateShippingFeeAsync(int restaurantId, double latitude, double longitude)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId);
            if (restaurant == null || string.IsNullOrEmpty(restaurant.Coordinate))
            {
                // Return a default shipping fee or throw an exception
                return 15000m;
            }

            var coords = restaurant.Coordinate.Split(',');
            if (coords.Length == 2 && double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lon))
            {
                var distance = CalculateDistance(lat, lon, latitude, longitude);
                // 10,000 VND base fee + 5,000 VND per km
                return 10000m + (decimal)(distance * 5000);
            }

            // Return a default shipping fee or throw an exception
            return 15000m;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radius of the earth in km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        private double ToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public async Task<decimal> GetSellerRevenueAsync(int sellerId)
        {
            var orders = await _orderRepository.GetPagedBySellerAsync(sellerId, 1, int.MaxValue, null);
            var completedOrders = orders.Items.Where(o => o.StatusHistory.Any(s => s.Status == "Completed")).ToList();

            decimal totalRevenue = 0;
            foreach (var order in completedOrders)
            {
                totalRevenue += order.FinalAmount - (order.TotalAmount * order.CommissionPercent / 100) - order.ShippingFee;
            }

            return totalRevenue;
        }

        public async Task<SellerStatsDto> GetSellerDashboardStatisticsAsync(int sellerId)
        {
            var orders = await _orderRepository.GetPagedBySellerAsync(sellerId, 1, int.MaxValue, null);
            var completedOrders = orders.Items.Where(o => o.StatusHistory.Any(s => s.Status == "Completed")).ToList();
            

            var totalRevenue = completedOrders.Sum(o => o.FinalAmount - (o.TotalAmount * o.CommissionPercent / 100) - o.ShippingFee);
            var totalOrders = completedOrders.Count();
            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
            var uniqueCustomers = completedOrders.Select(o => o.CustomerAccountId).Distinct().Count();

            return new SellerStatsDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageOrderValue = averageOrderValue,
                UniqueCustomers = uniqueCustomers
            };
        }

        public async Task<IEnumerable<RevenueOverTimeDto>> GetSellerRevenueOverTimeAsync(int sellerId, string period)
        {
            var orders = await _orderRepository.GetPagedBySellerAsync(sellerId, 1, int.MaxValue, null);
            var completedOrders = orders.Items.Where(o => o.StatusHistory.Any(s => s.Status == "Completed")).ToList();

            IEnumerable<RevenueOverTimeDto> result;

            if (period.ToLower() == "monthly")
            {
                result = completedOrders
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new RevenueOverTimeDto
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:00}",
                        Revenue = g.Sum(o => o.FinalAmount - (o.TotalAmount * o.CommissionPercent / 100) - o.ShippingFee)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
            }
            else // daily or default
            {
                result = completedOrders
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new RevenueOverTimeDto
                    {
                        Period = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(o => o.FinalAmount - (o.TotalAmount * o.CommissionPercent / 100) - o.ShippingFee)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
            }

            return result;
        }

        public async Task<IEnumerable<OrderStatusDistributionDto>> GetSellerOrderStatusDistributionAsync(int sellerId)
        {
            var orders = await _orderRepository.GetPagedBySellerAsync(sellerId, 1, int.MaxValue, null);
            

            return orders.Items
                .GroupBy(o => o.StatusHistory.OrderByDescending(s => s.CreatedAt).FirstOrDefault()?.Status ?? "Unknown")
                .Select(g => new OrderStatusDistributionDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();
        }

        public async Task<IEnumerable<PopularMenuItemDto>> GetSellerPopularMenuItemsAsync(int sellerId)
        {
            var orders = await _orderRepository.GetPagedBySellerAsync(sellerId, 1, int.MaxValue, null);
            var completedOrders = orders.Items.Where(o => o.StatusHistory.Any(s => s.Status == "Completed")).ToList();
            

            return orders.Items
                .Where(o => o.StatusHistory.Any(s => s.Status == "Completed"))
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => oi.MenuItem.Name)
                .Select(g => new PopularMenuItemDto
                {
                    MenuItemName = g.Key,
                    SalesCount = g.Sum(oi => oi.Qty)
                })
                .OrderByDescending(x => x.SalesCount)
                .Take(5) // Top 5 popular items
                .ToList();
        }
    }
}

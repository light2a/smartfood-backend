using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BLL.IServices;
using BLL.DTOs.Order;
using System.Threading.Tasks;
using System.Linq;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IOrderService orderService, ILogger<AdminController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        // ✅ Get paginated orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null)
        {
            var result = await _orderService.GetPagedAsync(pageNumber, pageSize, keyword);
            return Ok(result);
        }

        // ✅ Get details for one order
        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            try
            {
                var result = await _orderService.GetOrderDetailAsync(0, id); // Admin bypass
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminController] Failed to get order {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ Update order status
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var success = await _orderService.UpdateOrderStatusAsync(id, request.NewStatus, request.Note);
                return Ok(new { success, message = $"Order {id} updated to {request.NewStatus}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminController] Error updating order status for {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ Cancel order
        [HttpDelete("orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var success = await _orderService.UpdateOrderStatusAsync(id, "Cancelled", "Cancelled by admin.");
                return Ok(new { success, message = $"Order {id} cancelled by admin." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminController] Error cancelling order {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ Analytics Dashboard (with filters + top restaurants)
        [HttpGet("orders/summary")]
        public async Task<IActionResult> GetDashboardSummary(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            _logger.LogInformation("[AdminController] Generating admin order summary for date range: {From} - {To}", from, to);

            var paged = await _orderService.GetPagedAsync(1, 99999, null);
            var allOrders = paged.Items.ToList();

            if (!allOrders.Any())
                return Ok(new { totalOrders = 0, message = "No orders found." });

            // === Filter by date range ===
            if (from.HasValue)
                allOrders = allOrders.Where(o => o.CreatedAt.Date >= from.Value.Date).ToList();
            if (to.HasValue)
                allOrders = allOrders.Where(o => o.CreatedAt.Date <= to.Value.Date).ToList();

            if (!allOrders.Any())
                return Ok(new { totalOrders = 0, message = "No orders found for selected range." });

            // === Totals ===
            var totalOrders = allOrders.Count;
            var totalRevenue = allOrders.Sum(o => o.FinalAmount * 0.2m); // system revenue only
            var totalCompleted = allOrders.Count(o => o.StatusHistory.Any(s => s.Status == "Completed"));
            var totalCancelled = allOrders.Count(o => o.StatusHistory.Any(s => s.Status == "Cancelled"));
            var avgOrderValue = totalOrders > 0 ? allOrders.Average(o => o.FinalAmount) : 0;

            // === Group by Date ===
            var dailyStats = allOrders
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Orders = g.Count(),
                    Revenue = g.Sum(o => o.FinalAmount * 0.2m), // system revenue
                    Completed = g.Count(o => o.StatusHistory.Any(s => s.Status == "Completed")),
                    Cancelled = g.Count(o => o.StatusHistory.Any(s => s.Status == "Cancelled"))
                })
                .ToList();

            var restaurantStats = allOrders
                .GroupBy(o => o.RestaurantName)
                .Select(g => new
                {
                    Restaurant = g.Key,
                    Orders = g.Count(),
                    Revenue = g.Sum(o => o.FinalAmount * 0.2m), // system revenue
                    Completed = g.Count(o => o.StatusHistory.Any(s => s.Status == "Completed")),
                    Cancelled = g.Count(o => o.StatusHistory.Any(s => s.Status == "Cancelled"))
                })
                .OrderByDescending(g => g.Revenue)
                .ToList();


            // === Top 5 Restaurants ===
            var topRestaurants = restaurantStats.Take(5).ToList();

            return Ok(new
            {
                filters = new
                {
                    from = from?.ToString("yyyy-MM-dd") ?? "All",
                    to = to?.ToString("yyyy-MM-dd") ?? "All"
                },
                totals = new
                {
                    totalOrders,
                    totalCompleted,
                    totalCancelled,
                    totalRevenue,
                    avgOrderValue
                },
                byDate = dailyStats,
                byRestaurant = restaurantStats,
                topRestaurants
            });
        }

        [HttpGet("categories/popular")]
        public async Task<IActionResult> GetPopularCategories([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var data = await _orderService.GetCategoryPopularityAsync(from, to);
            return Ok(data);
        }

    }

    // ✅ DTO for updating order status
    public class UpdateOrderStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}

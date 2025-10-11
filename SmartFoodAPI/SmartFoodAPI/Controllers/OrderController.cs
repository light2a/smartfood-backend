using BLL.DTOs.Order;
using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Create a new order with multiple menu items and order type (Pickup or Delivery).
        /// </summary>
        /// <remarks>
        /// Example JSON request:
        /// 
        ///     POST /api/order/create
        ///     {
        ///       "orderType": "Delivery",
        ///       "items": [
        ///         { "menuItemId": 1, "quantity": 2 },
        ///         { "menuItemId": 3, "quantity": 1 }
        ///       ]
        ///     }
        /// </remarks>
        [HttpPost("create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                if (request == null || request.Items == null || !request.Items.Any())
                    return BadRequest(new { error = "Order must contain at least one item." });

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                       throw new Exception("Invalid token."));

                var result = await _orderService.CreateOrderAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{orderId}/status")]
        [Authorize(Roles = "admin,manager,seller")]
        public async Task<IActionResult> UpdateStatus(int orderId, [FromQuery] string newStatus, [FromQuery] string? note)
        {
            try
            {
                var success = await _orderService.UpdateOrderStatusAsync(orderId, newStatus, note);
                if (!success) return NotFound(new { error = "Order not found." });

                return Ok(new { message = $"Order {orderId} updated to status: {newStatus}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{orderId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                       throw new Exception("Invalid token."));

                var orderDetail = await _orderService.GetOrderDetailAsync(userId, orderId);
                return Ok(orderDetail);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("my-orders")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                       throw new Exception("Invalid token."));

                var orders = await _orderService.GetOrdersByCustomerAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{orderId}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                       throw new Exception("Invalid token."));

                var success = await _orderService.CancelOrderAsync(userId, orderId);
                if (!success)
                    return BadRequest(new { error = "Order cannot be cancelled. Only orders in 'Created' status can be cancelled." });

                return Ok(new { message = $"Order {orderId} has been cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

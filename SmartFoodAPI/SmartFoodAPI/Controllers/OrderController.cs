using BLL.DTOs.Order;
using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet("paging")]
        public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
        {
            var result = await _orderService.GetPagedAsync(pageNumber, pageSize, keyword);
            return Ok(result);
        }

        // 🚀 Updated endpoint with PayOS support
        [HttpPost("create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            _logger.LogInformation("CreateOrder endpoint called with request: {Request}", JsonSerializer.Serialize(request));
            
            // Log all claims in the User object
            if (User.Claims.Any())
            {
                _logger.LogInformation("Claims found in User object:");
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation("  Claim Type: {ClaimType}, Claim Value: {ClaimValue}", claim.Type, claim.Value);
                }
            }
            else
            {
                _logger.LogWarning("No claims found in User object.");
            }

            try
            {
                if (request == null || request.Items == null || !request.Items.Any())
                {
                    _logger.LogWarning("CreateOrder failed: Request body is null or contains no items.");
                    return BadRequest(new { error = "Order must contain at least one item." });
                }

                _logger.LogInformation("Attempting to parse user ID from token.");
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogError("CreateOrder failed: 'nameidentifier' claim is missing from the token.");
                    throw new Exception("Invalid token: 'nameidentifier' claim is missing.");
                }

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogError("CreateOrder failed: 'nameidentifier' claim '{sub}' is not a valid integer.", userIdClaim);
                    throw new Exception($"Invalid token: 'nameidentifier' claim '{userIdClaim}' is not a valid integer.");
                }
                _logger.LogInformation("Successfully parsed user ID: {UserId}", userId);

                _logger.LogInformation("Calling OrderService.CreateOrderAsync for user {UserId}", userId);
                var result = await _orderService.CreateOrderAsync(userId, request);

                // result now includes PaymentUrl from PayOS
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred in CreateOrder endpoint.");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{orderId}/status")]
        [Authorize(Roles = "admin,manager,seller,Seller")]
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
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new Exception("Invalid token."));

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
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new Exception("Invalid token."));

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
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new Exception("Invalid token."));

                var success = await _orderService.CancelOrderAsync(userId, orderId);
                if (!success)
                    return BadRequest(new { error = "Order cannot be cancelled." });

                return Ok(new { message = $"Order {orderId} has been cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("calculate-shipping")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CalculateShipping([FromBody] CalculateShippingFeeRequest request)
        {
            try
            {
                var fee = await _orderService.CalculateShippingFeeAsync(request.RestaurantId, request.Latitude, request.Longitude);
                return Ok(new { shippingFee = fee });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Seller")]
        [HttpGet("seller")]
        public async Task<IActionResult> GetSellerOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
        {
            try
            {
                var sellerIdClaim = User.FindFirst("SellerId")?.Value;
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing Seller ID." });

                int sellerId = int.Parse(sellerIdClaim);

                var result = await _orderService.GetPagedBySellerAsync(sellerId, pageNumber, pageSize, keyword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Seller")]
        [HttpGet("seller/revenue")]
        public async Task<IActionResult> GetSellerRevenue()
        {
            try
            {
                var sellerIdClaim = User.FindFirst("SellerId")?.Value;
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing Seller ID." });

                int sellerId = int.Parse(sellerIdClaim);

                var revenue = await _orderService.GetSellerRevenueAsync(sellerId);
                return Ok(new { revenue });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
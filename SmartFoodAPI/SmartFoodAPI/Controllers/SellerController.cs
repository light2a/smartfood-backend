using BLL.IServices;
using BLL.DTOs.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Seller")]
    public class SellerController : ControllerBase
    {
        private readonly ISellerService _sellerService;
        private readonly IOrderService _orderService;
        private readonly ILogger<SellerController> _logger;

        public SellerController(ISellerService sellerService, IOrderService orderService, ILogger<SellerController> logger)
        {
            _sellerService = sellerService;
            _orderService = orderService;
            _logger = logger;
        }

        // ===============================
        // ✅ Approve Seller (Admin use)
        // ===============================
        [AllowAnonymous]
        [HttpPut("approve-seller/{sellerId}")]
        public async Task<IActionResult> ApproveSeller(int sellerId)
        {
            try
            {
                await _sellerService.ApproveSellerAsync(sellerId);
                return Ok(new { message = "✅ Seller approved successfully and account activated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ===============================
        // ✅ Stripe Onboarding
        // ===============================
        [HttpGet("stripe/onboarding-link")]
        public async Task<IActionResult> GetStripeOnboardingLink()
        {
            try
            {
                var sellerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SellerId");
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Seller ID not found in token." });

                int sellerId = int.Parse(sellerIdClaim.Value);
                var onboardingUrl = await _sellerService.GenerateStripeOnboardingLinkAsync(sellerId);
                return Ok(new { url = onboardingUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ===============================
        // ✅ Get All Orders for Seller’s Restaurant(s)
        // ===============================
        [HttpGet("orders")]
        public async Task<IActionResult> GetSellerOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null)
        {
            try
            {
                var sellerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SellerId");
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Seller ID not found in token." });

                int sellerId = int.Parse(sellerIdClaim.Value);

                var result = await _orderService.GetPagedBySellerAsync(sellerId, pageNumber, pageSize, keyword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SellerController] Failed to get orders for seller");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ===============================
        // ✅ Get Single Order Detail (Seller scope)
        // ===============================
        [HttpGet("orders/{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            try
            {
                var sellerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SellerId");
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Seller ID not found in token." });

                int sellerId = int.Parse(sellerIdClaim.Value);
                var result = await _orderService.GetOrderDetailBySellerAsync(sellerId, orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SellerController] Failed to get order detail {OrderId}", orderId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ===============================
        // ✅ Update Order Status (Created → Preparing → Shipping → Completed)
        // ===============================
        [HttpPut("orders/{orderId}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var sellerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SellerId");
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Seller ID not found in token." });

                int sellerId = int.Parse(sellerIdClaim.Value);
                var success = await _orderService.UpdateOrderStatusBySellerAsync(sellerId, orderId, request.NewStatus, request.Note);

                return Ok(new { success, message = $"Order {orderId} updated to '{request.NewStatus}'." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SellerController] Failed to update order status for {OrderId}", orderId);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

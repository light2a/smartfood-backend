using BLL.DTOs.Order;
using BLL.DTOs.Seller;
using BLL.Extensions;
using BLL.IServices;
using DAL.Models;
using DAL.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Seller,seller")]
    public class SellerController : ControllerBase
    {
        private readonly ISellerService _sellerService;
        private readonly IOrderService _orderService;
        private readonly ILogger<SellerController> _logger;
        private readonly IPaymentService _paymentService;

        public SellerController(
            ISellerService sellerService,
            IOrderService orderService,
            ILogger<SellerController> logger,
            IPaymentService paymentService)
        {
            _sellerService = sellerService;
            _orderService = orderService;
            _logger = logger;
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSellerInfo()
        {
            try
            {
                var sellerIdClaim = User.FindFirst("SellerId")?.Value;
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing Seller ID." });

                int sellerId = int.Parse(sellerIdClaim);

                var sellerInfo = await _sellerService.GetSellerInfoAsync(sellerId);
                return Ok(sellerInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSellerInfo([FromBody] UpdateSellerInfoRequestDto dto)
        {
            try
            {
                var sellerIdClaim = User.FindFirst("SellerId")?.Value;
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing Seller ID." });

                int sellerId = int.Parse(sellerIdClaim);

                await _sellerService.UpdateSellerInfoAsync(sellerId, dto);
                return Ok(new { message = "Seller information updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ===============================
        // ✅ Get Bank List
        // ===============================
        [HttpGet("banks")]
        [AllowAnonymous]
        public IActionResult GetBankList()
        {
            var banks = Enum.GetValues(typeof(VietnameseBankCode))
                .Cast<VietnameseBankCode>()
                .Select(b => new BankDto { Name = b.GetDescription(), Value = b.ToString() })
                .ToList();
            return Ok(banks);
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

        // ===============================
        // ✅ Get Seller Bank Info
        // ===============================
        [HttpGet("bank-info")]
        public async Task<IActionResult> GetBankInfo()
        {
            try
            {
                var sellerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SellerId");
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Seller ID not found in token." });

                int sellerId = int.Parse(sellerIdClaim.Value);
                var bankInfo = await _sellerService.GetBankInfoAsync(sellerId);
                return Ok(bankInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SellerController] Failed to get bank info for seller");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ===============================
        // ✅ Update Seller Bank Info
        // ===============================
        [HttpPut("bank-info")]
        public async Task<IActionResult> UpdateBankInfo([FromBody] BLL.DTOs.Seller.UpdateSellerBankInfoRequestDto dto)
        {
            try
            {
                var sellerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SellerId");
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Seller ID not found in token." });

                int sellerId = int.Parse(sellerIdClaim.Value);
                await _sellerService.UpdateBankInfoAsync(sellerId, dto);
                return Ok(new { message = "Bank information updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SellerController] Failed to update bank info for seller");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("dashboard/statistics")]
        public async Task<IActionResult> GetDashboardStatistics()
        {
            try
            {
                var sellerIdClaim = User.FindFirst("SellerId")?.Value;
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing Seller ID." });

                int sellerId = int.Parse(sellerIdClaim);

                var stats = await _orderService.GetSellerDashboardStatisticsAsync(sellerId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("dashboard/revenue-over-time")]
        public async Task<IActionResult> GetRevenueOverTime([FromQuery] string period = "daily")
        {
            try
            {
                var sellerIdClaim = User.FindFirst("SellerId")?.Value;
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing Seller ID." });

                int sellerId = int.Parse(sellerIdClaim);

                var revenueData = await _orderService.GetSellerRevenueOverTimeAsync(sellerId, period);
                return Ok(revenueData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("dashboard/order-status-distribution")]
        public async Task<IActionResult> GetOrderStatusDistribution()
        {
            try
            {
                var sellerIdClaim = User.FindFirst("SellerId")?.Value;
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing Seller ID." });

                int sellerId = int.Parse(sellerIdClaim);

                var distribution = await _orderService.GetSellerOrderStatusDistributionAsync(sellerId);
                return Ok(distribution);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("dashboard/popular-menu-items")]
        public async Task<IActionResult> GetPopularMenuItems()
        {
            try
            {
                var sellerIdClaim = User.FindFirst("SellerId")?.Value;
                if (sellerIdClaim == null)
                    return Unauthorized(new { error = "Invalid token or missing Seller ID." });

                int sellerId = int.Parse(sellerIdClaim);

                var popularItems = await _orderService.GetSellerPopularMenuItemsAsync(sellerId);
                return Ok(popularItems);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }


}


using BLL.DTOs.Payment;
using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/payment")] // ✅ Fixed: lowercase "payment" để match với webhook URL
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost("create-order/{orderId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreatePayOsOrder(int orderId)
        {
            var url = await _paymentService.CreatePayOsOrderAsync(orderId);
            return Ok(new { paymentUrl = url });
        }

        [HttpPost("callback")]
        [AllowAnonymous] // ✅ Cho phép webhook từ PayOS gọi vào mà không cần auth
        public async Task<IActionResult> Callback([FromBody] PayOsWebhookDto callback)
        {
            _logger.LogInformation("=== WEBHOOK RECEIVED ===");
            _logger.LogInformation($"OrderCode: {callback?.data?.orderCode}");
            _logger.LogInformation($"Code: {callback?.code}");
            _logger.LogInformation($"Amount: {callback?.data?.amount}");

            // ✅ Validate input
            if (callback == null || callback.data == null)
            {
                _logger.LogError("Invalid webhook payload received");
                return BadRequest(new { message = "Invalid payload" });
            }

            try
            {
                var success = await _paymentService.HandleCallbackAsync(callback);

                if (!success)
                {
                    _logger.LogWarning($"Payment verification failed for order {callback.data.orderCode}");
                    return BadRequest(new { message = "Payment verification failed" });
                }

                _logger.LogInformation($"✅ Webhook processed successfully for order {callback.data.orderCode}");
                return Ok(new { message = "Payment processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing webhook for order {callback?.data?.orderCode}");
                // ✅ Trả về 200 OK để PayOS không retry (nếu đây là lỗi không thể sửa)
                // Hoặc 500 nếu muốn PayOS retry
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }

        // ✅ Thêm health check endpoint để test
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}
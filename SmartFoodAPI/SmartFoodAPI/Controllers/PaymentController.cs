using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Create a Stripe PaymentIntent for an order.
        /// </summary>
        [HttpPost("create-intent/{orderId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreatePaymentIntent(int orderId)
        {
            try
            {
                var clientSecret = await _paymentService.CreatePaymentIntentAsync(orderId);
                return Ok(new { clientSecret });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Confirm payment after success.
        /// </summary>
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromQuery] string paymentIntentId)
        {
            try
            {
                var success = await _paymentService.ConfirmPaymentAsync(paymentIntentId);
                if (!success)
                    return BadRequest(new { error = "Payment not successful." });

                return Ok(new { message = "Payment confirmed and order marked as Paid." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

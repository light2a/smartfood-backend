using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellerController : ControllerBase
    {
        private readonly ISellerService _sellerService;

        public SellerController(ISellerService sellerService)
        {
            _sellerService = sellerService;
        }

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

        [Authorize(Roles = "Seller")]
        [HttpGet("stripe/onboarding-link")]
        public async Task<IActionResult> GetStripeOnboardingLink()
        {
            try
            {
                // ✅ Extract SellerId from JWT token
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
    }
}

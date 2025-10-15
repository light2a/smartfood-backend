using BLL.IServices;
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
    }
}

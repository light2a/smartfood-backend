using BLL.DTOs.Feedback;
using BLL.IServices;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _service;
        private readonly ILogger<FeedbackController> _logger;
        public FeedbackController(IFeedbackService service, ILogger<FeedbackController> logger)
        {
            _service = service;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());
        [HttpGet("paging")]
        public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize, keyword);
            return Ok(result);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var feedback = await _service.GetByIdAsync(id);
            if (feedback == null) return NotFound();
            return Ok(feedback);
        }
        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            try
            {
                var feedback = await _service.GetByOrderIdAsync(orderId);
                if (feedback == null) return NotFound(new { message = "No feedback found for this order." });
                return Ok(feedback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FeedbackController] Error getting feedback for order {orderId}", orderId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request)
        {
            try
            {
                var created = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorCode = "PR50001", Message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string keyword)
        {
            var result = await _service.SearchAsync(keyword);
            return Ok(result);
        }
        [HttpGet("menuitems/{menuItemId}/feedback")]
        public async Task<IActionResult> GetFeedbackByMenuItem(int menuItemId)
        {
            try
            {
                var result = await _service.GetByMenuItemAsync(menuItemId);

                if (!result.Any())
                    return NotFound(new { message = "Không có feedback cho món ăn này." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MenuItemController] Error getting feedback for menu item");
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}


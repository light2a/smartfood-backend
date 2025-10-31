using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BLL.DTOs.Restaurant;
using System;
using System.Threading.Tasks;
using BLL.Services;

namespace SmartFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantsController : ControllerBase
    {
        private readonly IRestaurantService _service;

        public RestaurantsController(IRestaurantService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("paging")]
        public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize, keyword);
            return Ok(result);
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _service.GetByIdAsync(id);
            if (r == null) return NotFound();
            return Ok(r);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> Create([FromForm] CreateRestaurantRequest request, IFormFile? logo)
        {
            var created = await _service.CreateAsync(request, logo);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateRestaurantRequest request, IFormFile? logo)
        {
            await _service.UpdateAsync(id, request, logo);
            return NoContent();
        }

        // Xoá
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        // Search by name or address
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromQuery] string? keyword)
        {
            var list = await _service.SearchAsync(keyword);
            return Ok(list);
        }

        // Toggle IsActive
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> ToggleActive(int id, [FromQuery] bool isActive)
        {
            await _service.ToggleActiveAsync(id, isActive);
            return NoContent();
        }
        [HttpGet("search-by-menu")]
        public async Task<IActionResult> SearchByMenuItem([FromQuery] string keyword)
        {
            var result = await _service.SearchByMenuItemNameAsync(keyword);
            return Ok(result);
        }
    }
}

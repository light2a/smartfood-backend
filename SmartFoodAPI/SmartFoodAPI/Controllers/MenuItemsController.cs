using BLL.IServices;
using BLL.DTOs.MenuItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuItemService _service;

    public MenuItemsController(IMenuItemService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    //[Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> Create([FromForm] CreateMenuItemRequest request, IFormFile? logo)
    {
        var created = await _service.CreateAsync(request, logo);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    //[Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateMenuItemRequest request, IFormFile? logo)
    {
        await _service.UpdateAsync(id, request, logo);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    //[Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        var result = await _service.SearchAsync(keyword);
        return Ok(result);
    }
    [HttpPatch("{id:int}/availability")]
    //[Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> ToggleAvailability(int id, [FromQuery] bool isAvailable)
    {
        await _service.ToggleAvailabilityAsync(id, isAvailable);
        return NoContent();
    }

}

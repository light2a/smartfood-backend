using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.MenuItem;
using Microsoft.AspNetCore.Http;

namespace BLL.IServices
{
    public interface IMenuItemService
    {
        Task<IEnumerable<MenuItemDto>> GetAllAsync();
        Task<MenuItemDto?> GetByIdAsync(int id);
        Task<MenuItemDto> CreateAsync(CreateMenuItemRequest request, IFormFile? logo);
        Task UpdateAsync(int id, UpdateMenuItemRequest request, IFormFile? logo);
        Task DeleteAsync(int id);
        Task<IEnumerable<MenuItemDto>> SearchAsync(string keyword);
        Task ToggleAvailabilityAsync(int id, bool isAvailable);
    }
}

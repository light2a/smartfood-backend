using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface IMenuItemRepository
    {
        Task<IEnumerable<MenuItem>> GetAllAsync();
        Task<PagedResult<MenuItem>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<MenuItem?> GetByIdAsync(int id);
        Task<MenuItem> AddAsync(MenuItem item);
        Task UpdateAsync(MenuItem item);
        Task DeleteAsync(int id);
        Task<IEnumerable<MenuItem>> SearchAsync(string keyword);
        Task<IEnumerable<MenuItem>> GetByRestaurantAsync(int restaurantId);
        Task<IEnumerable<MenuItem>> GetByCategoryAsync(int categoryId);
        Task ToggleStatusAsync(int id, MenuItemStatus status);
    }
}

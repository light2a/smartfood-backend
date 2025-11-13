using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.Category;
using BLL.DTOs.MenuItem;
using DAL.Models;

namespace BLL.IServices
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<PagedResult<CategoryDto>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<CategoryDto?> GetByIdAsync(int id);
        //Task<CategoryDto?> GetByRestaurantAsync(int restaurantId);
        Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
        Task UpdateAsync(int id, UpdateCategoryRequest request);
        Task DeleteAsync(int id);
        Task<IEnumerable<CategoryDto>> SearchAsync(string keyword);
        Task<IEnumerable<CategoryDto>> GetByRestaurantAsync(int restaurantId);
    }
}

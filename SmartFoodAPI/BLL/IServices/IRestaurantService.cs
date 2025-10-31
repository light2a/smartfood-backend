using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.Restaurant;
using DAL.Models;
using Microsoft.AspNetCore.Http;

namespace BLL.IServices
{
    public interface IRestaurantService
    {
        Task<IEnumerable<RestaurantDto>> GetAllAsync();
        Task<PagedResult<RestaurantDto>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<RestaurantDto?> GetByIdAsync(int id);
        Task<RestaurantDto> CreateAsync(CreateRestaurantRequest request, IFormFile? logo);
        Task UpdateAsync(int id,UpdateRestaurantRequest request, IFormFile? logo);
        Task DeleteAsync(int id);
        Task<IEnumerable<RestaurantDto>> SearchAsync(string? keyword);
        Task ToggleActiveAsync(int id, bool isActive);
        Task<IEnumerable<RestaurantDto>> SearchByMenuItemNameAsync(string keyword);

    }
}

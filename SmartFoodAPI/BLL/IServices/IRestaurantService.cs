using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.Restaurant;

namespace BLL.IServices
{
    public interface IRestaurantService
    {
        Task<IEnumerable<RestaurantDto>> GetAllAsync();
        Task<RestaurantDto?> GetByIdAsync(int id);
        Task<RestaurantDto> CreateAsync(CreateRestaurantRequest request);
        Task UpdateAsync(int id,UpdateRestaurantRequest request);
        Task DeleteAsync(int id);
        Task<IEnumerable<RestaurantDto>> SearchAsync(string? keyword);
        Task ToggleActiveAsync(int id, bool isActive);
    }
}

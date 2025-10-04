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
        Task<RestaurantDto?> GetByIdAsync(Guid id);
        Task<RestaurantDto> CreateAsync(CreateRestaurantRequest request);
        Task UpdateAsync(Guid id,UpdateRestaurantRequest request);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<RestaurantDto>> SearchAsync(string? keyword);
        Task ToggleActiveAsync(Guid id, bool isActive);
    }
}

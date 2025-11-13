using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface IRestaurantRepository
    {
        Task<IEnumerable<Restaurant>> GetAllAsync();
        Task<PagedResult<Restaurant>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<Restaurant?> GetByIdAsync(int id);
        Task<Restaurant> AddAsync(Restaurant restaurant);
        Task UpdateAsync(Restaurant restaurant);
        Task DeleteAsync(int id);
        Task<IEnumerable<Restaurant>> SearchAsync(string? keyword);
        Task ToggleActiveAsync(int id, bool isActive);
        Task<IEnumerable<Restaurant>> SearchByMenuItemNameAsync(string keyword);
        //Task<IEnumerable<Restaurant>> ChangeActiveAsync(int areaId, bool isActive);
    }
}

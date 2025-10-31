using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface IAreaRepository
    {
        Task<IEnumerable<Area>> GetAllAsync();
        Task<PagedResult<Area>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
        Task<Area?> GetByIdAsync(int id);
        Task<Area> AddAsync(Area area);
        Task UpdateAsync(Area area);
        Task DeleteAsync(int id);
        Task<IEnumerable<Area>> SearchAsync(string keyword);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.Area;
using DAL.Models;

namespace BLL.IServices
{
    public interface IAreaService
    {
        Task<IEnumerable<AreaDto>> GetAllAsync();
        Task<PagedResult<AreaDto>> GetPagedAsync(int pageNumber, int pageSize, string keyword);
        Task<AreaDto?> GetByIdAsync(int id);
        Task<AreaDto> CreateAsync(CreateAreaRequest request);
        Task UpdateAsync(int id, UpdateAreaRequest request);
        Task DeleteAsync(int id);
        Task<IEnumerable<AreaDto>> SearchAsync(string keyword);
    }
}

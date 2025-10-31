using BLL.DTOs.Area;
using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;

namespace BLL.Services
{
    public class AreaService : IAreaService
    {
        private readonly IAreaRepository _repo;

        public AreaService(IAreaRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<AreaDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(a => new AreaDto
            {
                Id = a.Id,
                Name = a.Name,
                City = a.City
            });
        }

        public async Task<PagedResult<AreaDto>> GetPagedAsync(int pageNumber, int pageSize, string keyword)
        {
            var pagedResult = await _repo.GetPagedAsync(pageNumber, pageSize, keyword);
            return new PagedResult<AreaDto>
            {
                Items = pagedResult.Items.Select(a => new AreaDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    City = a.City
                }),
                TotalItems = pagedResult.TotalItems,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };
        }

        public async Task<AreaDto?> GetByIdAsync(int id)
        {
            var a = await _repo.GetByIdAsync(id);
            return a == null ? null : new AreaDto
            {
                Id = a.Id,
                Name = a.Name,
                City = a.City
            };
        }

        public async Task<AreaDto> CreateAsync(CreateAreaRequest request)
        {
            var area = new Area
            {
                Name = request.Name,
                City = request.City
            };

            var created = await _repo.AddAsync(area);
            return new AreaDto
            {
                Id = created.Id,
                Name = created.Name,
                City = created.City
            };
        }

        public async Task UpdateAsync(int id, UpdateAreaRequest request)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Area not found");

            existing.Name = request.Name;
            existing.City = request.City;

            await _repo.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }

        public async Task<IEnumerable<AreaDto>> SearchAsync(string keyword)
        {
            var list = await _repo.SearchAsync(keyword);
            return list.Select(a => new AreaDto
            {
                Id = a.Id,
                Name = a.Name,
                City = a.City
            });
        }
    }
}

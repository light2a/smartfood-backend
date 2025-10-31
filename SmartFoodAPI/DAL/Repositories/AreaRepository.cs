using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AreaRepository : IAreaRepository
    {
        private readonly SmartFoodContext _context;

        public AreaRepository(SmartFoodContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Area>> GetAllAsync() =>
            await _context.Areas.AsNoTracking().ToListAsync();

        public async Task<PagedResult<Area>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var query = _context.Areas.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(a => a.Name.Contains(keyword) ||
                                         (a.City != null && a.City.Contains(keyword)));
            }
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(a => a.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
            return new PagedResult<Area>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<Area?> GetByIdAsync(int id) =>
            await _context.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

        public async Task<Area> AddAsync(Area area)
        {
            _context.Areas.Add(area);
            await _context.SaveChangesAsync();
            return area;
        }

        public async Task UpdateAsync(Area area)
        {
            _context.Areas.Update(area);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.Areas.FindAsync(id);
            if (existing == null) throw new KeyNotFoundException("Area not found");
            _context.Areas.Remove(existing);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Area>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await _context.Areas.AsNoTracking().ToListAsync();

            return await _context.Areas
                .Where(a => a.Name.Contains(keyword) || (a.City != null && a.City.Contains(keyword)))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}

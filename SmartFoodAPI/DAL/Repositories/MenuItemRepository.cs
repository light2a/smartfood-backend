using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class MenuItemRepository : IMenuItemRepository
    {
        private readonly SmartFoodContext _context;

        public MenuItemRepository(SmartFoodContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MenuItem>> GetAllAsync() =>
            await _context.MenuItems.AsNoTracking().ToListAsync();

        public async Task<PagedResult<MenuItem>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var query = _context.MenuItems.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(r => r.Name.Contains(keyword) ||
                                         (r.Name != null && r.Name.Contains(keyword)));
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(r => r.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<MenuItem>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<MenuItem?> GetByIdAsync(int id) =>
            await _context.MenuItems.FindAsync(id);

        public async Task<MenuItem> AddAsync(MenuItem item)
        {
            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task UpdateAsync(MenuItem item)
        {
            _context.MenuItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.MenuItems.FindAsync(id);
            if (existing == null) throw new KeyNotFoundException("Menu item not found");
            _context.MenuItems.Remove(existing);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<MenuItem>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await _context.MenuItems.AsNoTracking().ToListAsync();

            return await _context.MenuItems
                .Where(m => m.Name.Contains(keyword) ||
                            (m.Description != null && m.Description.Contains(keyword)))
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<IEnumerable<MenuItem>> GetByRestaurantAsync(int restaurantId)
        {
            return await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.RestaurantId == restaurantId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetByCategoryAsync(int categoryId)
        {
            return await _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => m.CategoryId == categoryId)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task ToggleStatusAsync(int id, MenuItemStatus status)
        {
            var existing = await _context.MenuItems.FindAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Menu item not found");

            existing.Status = status;
            await _context.SaveChangesAsync();
        }
    }
}

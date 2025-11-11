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
    public class RestaurantRepository : IRestaurantRepository
    {
        private readonly SmartFoodContext _context;

        public RestaurantRepository(SmartFoodContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Restaurant>> GetAllAsync()
        {
            return await _context.Restaurants.Include(
                r => r.Area)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PagedResult<Restaurant>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var query = _context.Restaurants
                .Include(r => r.Seller) // load Seller
                .Include(r => r.Area)   // load Area
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(r =>
                    r.Name.Contains(keyword) ||
                    (r.Address != null && r.Address.Contains(keyword)) ||
                    (r.Seller != null && r.Seller.DisplayName.Contains(keyword)) ||
                    (r.Area != null && r.Area.Name.Contains(keyword))
                );
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderBy(r => r.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Restaurant>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<Restaurant?> GetByIdAsync(int id)
        {
            return await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Restaurant> AddAsync(Restaurant restaurant)
        {
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            return restaurant;
        }

        public async Task UpdateAsync(Restaurant restaurant)
        {
            _context.Restaurants.Update(restaurant);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.Restaurants.FindAsync(id);
            if (existing == null) throw new KeyNotFoundException("Restaurant not found");

            _context.Restaurants.Remove(existing);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Restaurant>> SearchAsync(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await _context.Restaurants.AsNoTracking().ToListAsync();

            return await _context.Restaurants
                .Where(r => r.Name.Contains(keyword) || (r.Address != null && r.Address.Contains(keyword)))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task ToggleActiveAsync(int id, bool isActive)
        {
            var existing = await _context.Restaurants.FindAsync(id);
            if (existing == null) throw new KeyNotFoundException("Restaurant not found");

            existing.IsActive = isActive;
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Restaurant>> SearchByMenuItemNameAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await _context.Restaurants.AsNoTracking().ToListAsync();

            return await _context.Restaurants
                .Where(r => r.MenuItems.Any(m => m.Name.Contains(keyword)))
                .AsNoTracking()
                .ToListAsync();
        }
        
    }
}


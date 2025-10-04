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
            return await _context.Restaurants.AsNoTracking().ToListAsync();
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
    }
}


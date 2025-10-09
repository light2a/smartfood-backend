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
    public class CategoryRepository : ICategoryRepository
    {
        private readonly SmartFoodContext _context;
        public CategoryRepository(SmartFoodContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllAsync() =>
           await _context.Categories.AsNoTracking().ToListAsync();

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.Restaurants)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Category> AddAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Categories.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Category not found");
            _context.Categories.Remove(entity);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Category>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await _context.Categories.AsNoTracking().ToListAsync();

            return await _context.Categories
                .Where(m => m.Name.Contains(keyword) ||
                            (m.Description != null && m.Description.Contains(keyword)))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}

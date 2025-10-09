using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.Category;
using BLL.DTOs.MenuItem;
using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;

namespace BLL.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;

        public CategoryService(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            });
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c == null) return null;

            return new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            };
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
        {
            var category = new Category
            {
                Name = request.Name,
                Description = request.Description
            };

            var created = await _repo.AddAsync(category);

            return new CategoryDto
            {
                Id = created.Id,
                Name = created.Name,
                Description = created.Description
            };
        }

        public async Task UpdateAsync(int id, UpdateCategoryRequest request)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Category not found");

            existing.Name = request.Name;
            existing.Description = request.Description;
            await _repo.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }

        public async Task<IEnumerable<CategoryDto>> SearchAsync(string keyword)
        {
            var list = await _repo.SearchAsync(keyword);
            return list.Select(m => new CategoryDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,

            });
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.MenuItem;
using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.AspNetCore.Http;

namespace BLL.Services
{
    public class MenuItemService : IMenuItemService
    {
        private readonly IMenuItemRepository _repo;
        private readonly IImageService _imageService;

        public MenuItemService(IMenuItemRepository repo, IImageService imageService)
        {
            _repo = repo;
            _imageService = imageService;
        }

        public async Task<IEnumerable<MenuItemDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(m => new MenuItemDto
            {
                Id = m.Id,
                RestaurantId = m.RestaurantId,
                CategoryId = m.CategoryId,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                Status = m.Status,
                LogoUrl = m.LogoUrl
            });
        }

        public async Task<PagedResult<MenuItemDto>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var pagedResult = await _repo.GetPagedAsync(pageNumber, pageSize, keyword);

            return new PagedResult<MenuItemDto>
            {
                Items = pagedResult.Items.Select(ToDto),
                TotalItems = pagedResult.TotalItems,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize
            };
        }

        public async Task<MenuItemDto?> GetByIdAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            return item == null ? null : new MenuItemDto
            {
                Id = item.Id,
                RestaurantId = item.RestaurantId,
                CategoryId = item.CategoryId,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Status = item.Status,
                LogoUrl = item.LogoUrl
            };
        }

        public async Task<MenuItemDto> CreateAsync(CreateMenuItemRequest request, IFormFile? logo)
        {
            string? logoUrl = null;
            if (logo != null)
                logoUrl = await _imageService.UploadAsync(logo);

            var item = new MenuItem
            {
                RestaurantId = request.RestaurantId,
                CategoryId = request.CategoryId,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Status = request.Status,
                LogoUrl = logoUrl
            };

            var created = await _repo.AddAsync(item);

            return new MenuItemDto
            {
                Id = created.Id,
                RestaurantId = created.RestaurantId,
                CategoryId = created.CategoryId,
                Name = created.Name,
                Description = created.Description,
                Price = created.Price,
                Status = created.Status,
                LogoUrl = created.LogoUrl
            };
        }

        public async Task UpdateAsync(int id, UpdateMenuItemRequest request, IFormFile? logo)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Menu item not found");

            existing.RestaurantId = request.RestaurantId;
            existing.CategoryId = request.CategoryId;
            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.Price = request.Price;
            existing.Status = request.Status;
            existing.RestaurantId = request.RestaurantId;

            if (logo != null)
            {
                var imageUrl = await _imageService.UploadAsync(logo);
                existing.LogoUrl = imageUrl;
            }

            await _repo.UpdateAsync(existing);
        }


        public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);
        public async Task<IEnumerable<MenuItemDto>> SearchAsync(string keyword)
        {
            var list = await _repo.SearchAsync(keyword);
            return list.Select(m => new MenuItemDto
            {
                Id = m.Id,
                RestaurantId = m.RestaurantId,
                CategoryId = m.CategoryId,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                Status = m.Status,
                LogoUrl = m.LogoUrl
            });
        }
        public async Task ToggleStatusAsync(int id, MenuItemStatus status)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Menu item not found");

            existing.Status = status;
            await _repo.UpdateAsync(existing);
        }
        public async Task<IEnumerable<MenuItemDto>> GetByRestaurantAsync(int restaurantId)
        {
            var list = await _repo.GetByRestaurantAsync(restaurantId);
            return list.Select(ToDto);
        }

        public async Task<IEnumerable<MenuItemDto>> GetByCategoryAsync(int categoryId)
        {
            var list = await _repo.GetByCategoryAsync(categoryId);
            return list.Select(ToDto);
        }
        private MenuItemDto ToDto(MenuItem m) => new()
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            Price = m.Price,
            Status = m.Status,
            LogoUrl = m.LogoUrl
        };
    }
}

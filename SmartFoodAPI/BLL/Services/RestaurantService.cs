using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.Restaurant;
using BLL.IServices;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.AspNetCore.Http;

namespace BLL.Services
{
    public class RestaurantService : IRestaurantService
    {
        private readonly IRestaurantRepository _repo;
        private readonly IImageService _imageService;

        public RestaurantService(IRestaurantRepository repo, IImageService imageService)
        {
            _repo = repo;
            _imageService = imageService;
        }

        public async Task<IEnumerable<RestaurantDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(ToDto);
        }

        public async Task<RestaurantDto?> GetByIdAsync(int id)
        {
            var r = await _repo.GetByIdAsync(id);
            return r == null ? null : ToDto(r);
        }

        public async Task<RestaurantDto> CreateAsync(CreateRestaurantRequest request, IFormFile? logo = null)
        {
            string? logoUrl = null;
            if (logo != null)
                logoUrl = await _imageService.UploadAsync(logo);

            var restaurant = new Restaurant
            {
                SellerId = request.SellerId,
                AreaId = request.AreaId,
                CategoryId = request.CategoryId,
                Name = request.Name,
                Address = request.Address,
                Coordinate = request.Coordinate,
                OpenTime = request.OpenTime,
                CloseTime = request.CloseTime,
                Hotline = request.Hotline,
                LogoUrl = logoUrl
            };

            var created = await _repo.AddAsync(restaurant);
            return new RestaurantDto
            {
                Id = created.Id,
                SellerId = created.SellerId,
                AreaId = created.AreaId,
                CategoryId = created.CategoryId,
                Name = created.Name,
                Address = created.Address,
                Coordinate = created.Coordinate,
                OpenTime = created.OpenTime,
                CloseTime = created.CloseTime,
                Hotline = created.Hotline,
                IsActive = created.IsActive,
                LogoUrl = created.LogoUrl
            };
        }

        public async Task UpdateAsync(int id, UpdateRestaurantRequest request, IFormFile? logo = null)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException("Restaurant not found");

            existing.SellerId = request.SellerId;
            existing.Name = request.Name;
            existing.Address = request.Address;
            existing.AreaId = request.AreaId;
            existing.Coordinate = request.Coordinate;
            existing.OpenTime = request.OpenTime;
            existing.CloseTime = request.CloseTime;
            existing.Hotline = request.Hotline;
            existing.IsActive = request.IsActive;

            if (logo != null)
                existing.LogoUrl = await _imageService.UploadAsync(logo);

            await _repo.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }

        private RestaurantDto ToDto(Restaurant r) => new RestaurantDto
        {
            Id = r.Id,
            SellerId = r.SellerId,
            AreaId = r.AreaId,
            Name = r.Name,
            Address = r.Address,
            IsActive = r.IsActive
        };
        public async Task<IEnumerable<RestaurantDto>> SearchAsync(string? keyword)
        {
            var list = await _repo.SearchAsync(keyword);
            return list.Select(ToDto);
        }

        public async Task ToggleActiveAsync(int id, bool isActive)
        {
            await _repo.ToggleActiveAsync(id, isActive);
        }
        public async Task<IEnumerable<RestaurantDto>> SearchByMenuItemNameAsync(string keyword)
        {
            var restaurants = await _repo.SearchByMenuItemNameAsync(keyword);

            return restaurants.Select(r => new RestaurantDto
            {
                Id = r.Id,
                Name = r.Name,
                Address = r.Address,
                IsActive = r.IsActive,
                LogoUrl = r.LogoUrl
            });
        }
    }
}

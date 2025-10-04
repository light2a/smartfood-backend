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

        public async Task<RestaurantDto> CreateAsync(CreateRestaurantRequest request, IFormFile? logo)
        {
            string? logoUrl = null;
            if (logo != null && logo.Length > 0)
            {
                logoUrl = await _imageService.UploadAsync(logo);
            }
            var restaurant = new Restaurant
            {
                SellerId = request.SellerId,
                AreaId = request.AreaId,
                Name = request.Name,
                Address = request.Address,
                LogoUrl = logoUrl,
                IsActive = true
            };

            var created = await _repo.AddAsync(restaurant);
            return new RestaurantDto
            {
                Id = created.Id,
                Name = created.Name,
                Address = created.Address,
                LogoUrl = created.LogoUrl,
                IsActive = created.IsActive
            };
        }

        public async Task UpdateAsync(int id, UpdateRestaurantRequest request, IFormFile? logo)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException("Restaurant not found");

            existing.Name = request.Name;
            existing.Address = request.Address;
            existing.IsActive = request.IsActive;
            existing.AreaId = request.AreaId;
            existing.SellerId = request.SellerId;
            if (logo != null)
            {
                var logoUrl = await _imageService.UploadAsync(logo);
                existing.LogoUrl = logoUrl;
            }
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
    }
}

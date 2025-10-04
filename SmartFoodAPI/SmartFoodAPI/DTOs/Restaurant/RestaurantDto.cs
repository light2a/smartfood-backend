using System;

namespace SmartFoodAPI.DTOs.Restaurant
{
    public class RestaurantDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public Guid? AreaId { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }
}

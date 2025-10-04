using System;

namespace BLL.DTOs.Restaurant
{
    public class RestaurantDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public int? AreaId { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public bool IsActive { get; set; }
    }
}

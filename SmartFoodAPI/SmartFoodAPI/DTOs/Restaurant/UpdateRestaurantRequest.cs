using System;
using System.ComponentModel.DataAnnotations;

namespace SmartFoodAPI.DTOs.Restaurant
{
    public class UpdateRestaurantRequest
    {
        [Required]
        public int SellerId { get; set; }
        public int? AreaId { get; set; }
        [Required, MaxLength(250)]
        public string Name { get; set; } = null!;
        [MaxLength(500)]
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

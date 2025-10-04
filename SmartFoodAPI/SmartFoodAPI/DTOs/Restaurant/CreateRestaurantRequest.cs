using System;
using System.ComponentModel.DataAnnotations;

namespace SmartFoodAPI.DTOs.Restaurant
{
    public class CreateRestaurantRequest
    {
        [Required]
        public Guid SellerId { get; set; }
        public Guid? AreaId { get; set; }
        [Required, MaxLength(250)]
        public string Name { get; set; } = null!;
        [MaxLength(500)]
        public string? Address { get; set; }
    }
}

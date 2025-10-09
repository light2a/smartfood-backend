using System;
using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Restaurant
{
    public class CreateRestaurantRequest
    {
        [Required]
        public int SellerId { get; set; }
        public int? AreaId { get; set; }
        [Required]
        public List<int>? CategoryIds { get; set; }
        [Required, MaxLength(250)]
        public string Name { get; set; } = null!;
        [MaxLength(500)]
        public string? Address { get; set; }
        [MaxLength(200)]
        public string? Coordinate { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        [MaxLength(20)]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Hotline phải là số điện thoại hợp lệ (bắt đầu bằng 0, có 10–11 chữ số).")]
        public string? Hotline { get; set; }
    }
}

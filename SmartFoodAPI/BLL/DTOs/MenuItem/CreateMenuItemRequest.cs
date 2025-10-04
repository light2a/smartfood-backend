using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.MenuItem
{
    public class CreateMenuItemRequest
    {
        [Required]
        public int RestaurantId { get; set; }
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá món ăn phải lớn hơn 0.")]
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}

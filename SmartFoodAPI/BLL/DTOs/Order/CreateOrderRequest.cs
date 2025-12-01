using DAL.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Order
{
    public class CreateOrderItemRequest
    {
        [Required]
        public int MenuItemId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    public class CreateOrderRequest
    {
        [Required]
        public List<CreateOrderItemRequest> Items { get; set; } = new();

        // ✅ Enum automatically appears as dropdown in Swagger
        [Required]
        public OrderType OrderType { get; set; } = OrderType.Pickup;

        public string? DeliveryAddress { get; set; }
        public double? DeliveryLatitude { get; set; }
        public double? DeliveryLongitude { get; set; }
    }
}

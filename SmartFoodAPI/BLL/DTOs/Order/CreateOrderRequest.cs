using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [Required]
        [RegularExpression("^(Pickup|Delivery)$", ErrorMessage = "OrderType must be either 'Pickup' or 'Delivery'.")]
        public string OrderType { get; set; } = "Pickup"; // default = pickup
    }
}

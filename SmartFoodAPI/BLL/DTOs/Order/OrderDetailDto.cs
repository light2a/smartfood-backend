using System;
using System.Collections.Generic;

namespace BLL.DTOs.Order
{
    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public int CustomerAccountId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime CreatedAt { get; set; }

        public string RestaurantName { get; set; }
        public string? RestaurantAddress { get; set; }

        public List<OrderItemDetailDto> Items { get; set; } = new();
        public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    }

    public class OrderItemDetailDto
    {
        public string MenuItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class OrderStatusHistoryDto
    {
        public string Status { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

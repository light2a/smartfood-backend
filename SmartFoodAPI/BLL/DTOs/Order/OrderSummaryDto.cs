using System;

namespace BLL.DTOs.Order
{
    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string LatestStatus { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int CustomerAccountId { get; set; } // FK to Account
        public int   RestaurantId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal FinalAmount { get; set; }
        public string OrderType { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Account Customer { get; set; }
        public virtual Restaurant Restaurant { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    }
}

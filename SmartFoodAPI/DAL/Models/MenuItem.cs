using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;

        public virtual Restaurant Restaurant { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}

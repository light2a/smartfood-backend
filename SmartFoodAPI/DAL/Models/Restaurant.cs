using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Restaurant
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public int? AreaId { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Seller Seller { get; set; }
        public virtual Area? Area { get; set; }
        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}

using System;
using System.Collections.Generic;

namespace DAL.Models
{
    public class Category
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public virtual Restaurant Restaurant { get; set; } = null!;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Seller
    {
        public int Id { get; set; }
        public int UserAccountId { get; set; } // FK to Account
        public string DisplayName { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public SellerStatus Status { get; set; } = SellerStatus.Unavailable;

        public virtual Account User { get; set; }
        public virtual ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
    }
}

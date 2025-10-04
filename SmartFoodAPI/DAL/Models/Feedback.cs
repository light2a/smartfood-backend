using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public int CustomerAccountId { get; set; } // FK to Account
        public int? RestaurantId { get; set; }
        public int? MenuItemId { get; set; }
        public int Rating { get; set; } // 1-5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Account Customer { get; set; }
        public virtual Restaurant? Restaurant { get; set; }
        public virtual MenuItem? MenuItem { get; set; }
    }
}

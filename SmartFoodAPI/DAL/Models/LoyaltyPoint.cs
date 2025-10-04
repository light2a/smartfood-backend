using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class LoyaltyPoint
    {
        public int Id { get; set; }
        public int UserAccountId { get; set; } // FK to Account
        public int Points { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual Account User { get; set; }
    }
}

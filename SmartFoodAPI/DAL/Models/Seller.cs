using DAL.Models.Enums;
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
        public int UserAccountId { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public SellerStatus Status { get; set; } = SellerStatus.Unavailable;

        // PayOS payout info
        public string? BankAccountNumber { get; set; }
        public VietnameseBankCode? BankCode { get; set; }

        public virtual Account User { get; set; }
        public virtual ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
    }


}

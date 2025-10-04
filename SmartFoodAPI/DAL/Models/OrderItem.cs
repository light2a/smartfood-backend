using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        public int Qty { get; set; } = 1;
        public decimal UnitPrice { get; set; }

        public virtual Order Order { get; set; }
        public virtual MenuItem MenuItem { get; set; }
    }
}

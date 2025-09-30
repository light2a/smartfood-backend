using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public partial class Role
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; }

        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}

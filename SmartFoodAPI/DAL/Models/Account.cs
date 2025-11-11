using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public partial class Account
    {
        public int AccountId { get; set; }

        public string Email { get; set; }

        public string FullName { get; set; }

        public string Password { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        public int? RoleId { get; set; }

        public string PhoneNumber { get; set; }

        public virtual Role Role { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public string? ExternalProvider { get; set; }
        public string? ExternalProviderKey { get; set; }
    }
}

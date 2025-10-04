using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(string email, string password);
        Task<Account> RegisterAsync(string fullName, string email, string password, string phonenumber);
        Task<Account> RegisterSellerAsync(string fullName, string email, string password, string phonenumber, string displayName);
    }
}

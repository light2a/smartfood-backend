using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IAccountRepository
    {
        Task<Account?> GetByEmailAsync(string email);
        Task<Account> AddAsync(Account account);
        Task<Account> UpdateAsync(Account account);
        Task<Account?> GetByIdAsync(int id);
        Task<Account> DeactivateAccountAsync(int id);
        Task<Account> UpdateAccountInfoAsync(int id, string fullName, string phoneNumber);
    }
}

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
        IQueryable<Account> GetAll();
        Task<PagedResult<Account>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);

        Task<Account?> GetByEmailAsync(string email);
        Task<Account> AddAsync(Account account);
        Task<Account> UpdateAsync(Account account);
        Task<Account?> GetByIdAsync(int id);
        Task<Account> DeactivateAccountAsync(int id);
        Task<Account> UpdateAccountInfoAsync(int id, string fullName, string phoneNumber);
        Task BanAccountAsync(int accountId, bool isBanned);
    }
}

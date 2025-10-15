using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly SmartFoodContext _context;

        public AccountRepository(SmartFoodContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<Account?> GetByIdAsync(int id)
        {
            return await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.AccountId == id);
        }

        public async Task<Account> AddAsync(Account account)
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<Account> UpdateAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            return account;
        }
        public async Task<Account> DeactivateAccountAsync(int id)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == id);
            if (account == null)
                throw new Exception("Account not found.");

            account.IsActive = false;
            account.UpdateAt = DateTime.UtcNow;

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<Account> UpdateAccountInfoAsync(int id, string fullName, string phoneNumber)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == id);
            if (account == null)
                throw new Exception("Account not found.");

            account.FullName = fullName;
            account.PhoneNumber = phoneNumber;
            account.UpdateAt = DateTime.UtcNow;

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            return account;
        }

    }
}

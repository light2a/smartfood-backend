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

        public IQueryable<Account> GetAll()
        {
            return _context.Accounts.Include(a => a.Role);
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

        public async Task BanAccountAsync(int accountId, bool isBanned)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null)
                throw new Exception("Account not found.");

            account.IsBanned = isBanned;
            account.UpdateAt = DateTime.UtcNow;

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }
        public async Task<PagedResult<Account>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var query = _context.Accounts.Include(a => a.Role).AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(a => a.Email.Contains(keyword) ||
                                         a.FullName.Contains(keyword) ||
                                         (a.PhoneNumber != null && a.PhoneNumber.Contains(keyword)));
            }
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(a => a.AccountId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
            return new PagedResult<Account>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}

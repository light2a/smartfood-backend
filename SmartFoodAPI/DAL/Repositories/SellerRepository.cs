using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class SellerRepository : ISellerRepository
    {
        private readonly SmartFoodContext _context;

        public SellerRepository(SmartFoodContext context)
        {
            _context = context;
        }

        public async Task<Seller> AddAsync(Seller seller)
        {
            _context.Sellers.Add(seller);
            await _context.SaveChangesAsync();
            return seller;
        }

        public async Task<Seller?> GetByIdAsync(int id)
        {
            return await _context.Sellers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Seller>> GetAllAsync()
        {
            return await _context.Sellers.Include(s => s.User).ToListAsync();
        }

        public async Task UpdateAsync(Seller seller)
        {
            _context.Sellers.Update(seller);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var seller = await GetByIdAsync(id);
            if (seller != null)
            {
                _context.Sellers.Remove(seller);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Seller?> GetByUserAccountIdAsync(int accountId)
        {
            return await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserAccountId == accountId);
        }

        public async Task ApproveSellerAsync(int sellerId)
        {
            var seller = await _context.Sellers.Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sellerId);

            if (seller == null)
                throw new Exception("Seller not found.");

            seller.Status = SellerStatus.Available;

            if (seller.User != null)
                seller.User.IsActive = true; // ✅ Activate the linked account too

            await _context.SaveChangesAsync();
        }
        public async Task<Seller?> GetByStripeAccountIdAsync(string stripeAccountId)
        {
            return await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StripeAccountId == stripeAccountId);
        }

        public async Task<PagedResult<Order>> GetPagedBySellerAsync(int sellerId, int pageNumber, int pageSize, string? keyword)
        {
            var query = _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.StatusHistory)
                .Where(o => o.Restaurant.SellerId == sellerId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(o =>
                    o.Customer.FullName.Contains(keyword) ||
                    o.Restaurant.Name.Contains(keyword));
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<Order>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }


    }
}

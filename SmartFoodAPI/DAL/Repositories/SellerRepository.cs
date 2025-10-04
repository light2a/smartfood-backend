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
    }
}

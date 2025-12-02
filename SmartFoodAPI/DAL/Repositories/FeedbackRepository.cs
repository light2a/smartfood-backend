using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly SmartFoodContext _context;
        public FeedbackRepository(SmartFoodContext context)
        {
            _context = context;
        }
        public async Task<Feedback> AddAsync(Feedback feedback)
        {
            _context.Feedbacks.Add(feedback);
            await  _context.SaveChangesAsync();
            return feedback;
        }

        public async Task DeleteAsync(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Feedback>> GetAllAsync()
        {
            return await _context.Feedbacks.AsQueryable().ToListAsync();
        }

        public async Task<Feedback?> GetByIdAsync(int id)
        {
            return await _context.Feedbacks.FindAsync(id);
        }

        public async Task<PagedResult<Feedback>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var query = _context.Feedbacks.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(f => f.Comment.Contains(keyword));
            }
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(f => f.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
            return new PagedResult<Feedback>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        public async Task<IEnumerable<Feedback>> SearchAsync(string keyword)
        {
            return await _context.Feedbacks
                .Where(f => f.Comment.Contains(keyword))
                .ToListAsync();
        }
        //public async Task UpdateAsync(Feedback feedback)
        //{
        //    _context.Feedbacks.Update(feedback);
        //    await _context.SaveChangesAsync();
        //}
        public async Task<IEnumerable<Feedback>> GetByMenuItemAsync(int menuItemId)
        {
            return await _context.Feedbacks
                .Include(f => f.Order)
                    .ThenInclude(o => o.OrderItems)
                .Include(f => f.Customer)
                .Where(f => f.Order.OrderItems.Any(oi => oi.MenuItemId == menuItemId))
                .AsNoTracking()
                .ToListAsync();
        }

    }
}

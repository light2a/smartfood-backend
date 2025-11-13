using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly SmartFoodContext _context;

        public OrderRepository(SmartFoodContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Order>> GetPagedAsync(int pageNumber, int pageSize, string? keyword)
        {
            var query = _context.Orders.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(o => o.Customer.FullName.Contains(keyword) ||
                                         o.Restaurant.Name.Contains(keyword));
            }
            var totalItems = await query.CountAsync();
            var items = await query
                .Include(o => o.Restaurant)
                .OrderBy(o => o.Id)
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

        public async Task<Order> AddAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task<Order?> GetDetailByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerAccountId)
        {
            return await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.StatusHistory)
                .Where(o => o.CustomerAccountId == customerAccountId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
        public async Task<List<Order>> GetAllOrdersWithItemsAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi.Category)
                .Include(o => o.StatusHistory)
                .ToListAsync();
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

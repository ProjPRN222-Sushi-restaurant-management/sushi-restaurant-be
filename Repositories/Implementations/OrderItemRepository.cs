using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly RestaurantSystemDbContext _context;

        public OrderItemRepository(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<OrderItem>> GetAllOrderItemsAsync(
            CancellationToken ct = default)
        {
            return await _context.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .Include(oi => oi.Order)
                .ToListAsync(ct);
        }

        public async Task<OrderItem> GetOrderItemByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var orderItem = await _context.OrderItems
                .AsNoTracking()
                .Include(oi => oi.MenuItem)
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == id, ct);

            return orderItem ?? throw new KeyNotFoundException($"Order item {id} not found.");
        }
    }
}

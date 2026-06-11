using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class OrderRepository : IOrderRepository
    {
        private readonly RestaurantSystemDbContext _context;

        public OrderRepository(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Order>> GetAllOrdersAsync(
            CancellationToken ct = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.Booking)
                .OrderByDescending(o => o.OrderId)
                .ToListAsync(ct);
        }

        public async Task<Order> GetOrderById(
            int id,
            CancellationToken ct = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.Booking)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == id, ct);

            return order ?? throw new KeyNotFoundException($"Order {id} not found.");
        }

        public async Task AddOrderAsync(Order order, CancellationToken ct = default)
        {
            await _context.Orders.AddAsync(order, ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<Order>> GetOrdersByBookingIdAsync(long bookingId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(od => od.MenuItem)
                .Where(o => o.BookingId == bookingId)
                .ToListAsync();
        }
    }
}

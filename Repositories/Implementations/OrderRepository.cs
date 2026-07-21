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
                .Include(o => o.InvoiceStaff)
                .Include(o => o.ReceivedStaff)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Customer)
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
                .Include(o => o.InvoiceStaff)
                .Include(o => o.ReceivedStaff)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == id, ct);

            return order ?? throw new KeyNotFoundException($"Order {id} not found.");
        }

        public async Task AddOrderAsync(Order order, CancellationToken ct = default)
        {
            await _context.Orders.AddAsync(order, ct);
        }

        public async Task<bool> UpdateOrderAsync(Order order, CancellationToken ct = default)
        {
            var existingOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId, ct);

            if (existingOrder == null)
            {
                return false;
            }

            existingOrder.BookingId = order.BookingId;
            existingOrder.CustomerId = order.CustomerId;
            existingOrder.TableId = order.TableId;
            existingOrder.OrderStatus = order.OrderStatus;
            existingOrder.TotalAmount = order.TotalAmount;
            existingOrder.CreatedAt = order.CreatedAt;
            existingOrder.CompletedAt = order.CompletedAt;
            existingOrder.ReceivedStaffId = order.ReceivedStaffId;
            existingOrder.ReceivedStaffName = order.ReceivedStaffName;
            existingOrder.ReceivedAt = order.ReceivedAt;
            existingOrder.InvoiceStaffId = order.InvoiceStaffId;
            existingOrder.InvoiceStaffName = order.InvoiceStaffName;
            existingOrder.InvoiceIssuedAt = order.InvoiceIssuedAt;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<Order>> GetOrdersByBookingIdAsync(long bookingId)
        {
            return await _context.Orders
                .Include(o => o.InvoiceStaff)
                .Include(o => o.ReceivedStaff)
                .Include(o => o.OrderItems)
                    .ThenInclude(od => od.MenuItem)
                .Where(o => o.BookingId == bookingId)
                .ToListAsync();
        }
    }
}

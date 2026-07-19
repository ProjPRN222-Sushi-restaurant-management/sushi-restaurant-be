using BusinessObjects.Enums;
using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class BookingRepository : IBookingRepository
    {
        private readonly RestaurantSystemDbContext _context;
        public BookingRepository(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        public async Task<Booking> GetBookingByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Table)
                .FirstOrDefaultAsync(b => b.BookingId == id, ct);
        }

        public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct = default)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Table)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Booking>> GetByDateAsync(
            DateOnly bookingDate,
            CancellationToken ct = default)
        {
            return await _context.Bookings
                .Where(b => b.BookingDate == bookingDate)
                .Include(b => b.Customer)
                .Include(b => b.Table)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Booking>> GetByStatusAsync(
            BookingStatusEnum status,
            CancellationToken ct = default)
        {
            return await _context.Bookings
                .Where(b => b.BookingStatus == status)
                .Include(b => b.Customer)
                .Include(b => b.Table)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Booking booking, CancellationToken ct = default)
        {
            await _context.Bookings.AddAsync(booking, ct);
        }

        public Task UpdateAsync(Booking booking, CancellationToken ct = default)
        {
            _context.Bookings.Update(booking);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long bookingId, CancellationToken ct = default)
        {
            var booking = await _context.Bookings.FindAsync(new object[] { bookingId }, ct);

            if (booking != null)
            {
                _context.Bookings.Remove(booking);
            }
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsAsync(
            long bookingId,
            CancellationToken ct = default)
        {
            return await _context.Bookings
                .AnyAsync(b => b.BookingId == bookingId, ct);
        }
    }
}

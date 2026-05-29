using BusinessObjects.Enums;
using BusinessObjects.Models;
using System.Numerics;

namespace Repositories.Interfaces
{
    internal interface IBookingRepository
    {
        Task<Booking> GetBookingByIdAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct = default);
        Task<IEnumerable<Booking>> GetByDateAsync(DateOnly bookingDate, CancellationToken ct = default);
        Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatusEnum status, CancellationToken ct = default);
        Task<bool> ExistsAsync(long bookingId, CancellationToken ct = default);
    }
}

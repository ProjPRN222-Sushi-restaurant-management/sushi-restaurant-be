using BusinessObjects.Enums;
using BusinessObjects.Models;
using DataAccessObjects.Requests;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default);
    Task<List<RestaurantTable>> GetAvailableTablesAsync(CreateBookingRequest request, CancellationToken ct = default);
    Task UpdateBookingStatusAsync(long bookingId, BookingStatusEnum status, CancellationToken ct = default);
    Task<Booking> GetBookingByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct = default);
    Task<IEnumerable<Booking>> GetByDateAsync(DateOnly bookingDate, CancellationToken ct = default);
    Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatusEnum status, CancellationToken ct = default);
    Task UpdateAsync(Booking booking, CancellationToken ct = default);
    Task DeleteAsync(long bookingId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(long bookingId, CancellationToken ct = default);
    Task<BookingOrderHistoryResult> GetBookingOrderHistoryAsync(long bookingId, CancellationToken ct = default);
}
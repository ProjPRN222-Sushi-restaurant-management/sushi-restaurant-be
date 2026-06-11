using BusinessObjects.Enums;
using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public Task<Booking> GetBookingByIdAsync(long id, CancellationToken ct = default)
        => _bookingRepository.GetBookingByIdAsync((int)id, ct);

    public Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct = default)
        => _bookingRepository.GetAllBookingsAsync(ct);

    public Task<IEnumerable<Booking>> GetByDateAsync(DateOnly bookingDate, CancellationToken ct = default)
        => _bookingRepository.GetByDateAsync(bookingDate, ct);

    public Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatusEnum status, CancellationToken ct = default)
        => _bookingRepository.GetByStatusAsync(status, ct);

    public Task AddAsync(Booking booking, CancellationToken ct = default)
        => _bookingRepository.AddAsync(booking, ct);

    public Task UpdateAsync(Booking booking, CancellationToken ct = default)
        => _bookingRepository.UpdateAsync(booking, ct);

    public Task DeleteAsync(long bookingId, CancellationToken ct = default)
        => _bookingRepository.DeleteAsync(bookingId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _bookingRepository.SaveChangesAsync(ct);

    public Task<bool> ExistsAsync(long bookingId, CancellationToken ct = default)
        => _bookingRepository.ExistsAsync(bookingId, ct);
}

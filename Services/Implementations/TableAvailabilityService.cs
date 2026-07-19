using BusinessObjects.Enums;
using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class TableAvailabilityService : ITableAvailabilityService
{
    private readonly IRestaurantTableService _tableService;
    private readonly IBookingService _bookingService;

    public TableAvailabilityService(
        IRestaurantTableService tableService,
        IBookingService bookingService)
    {
        _tableService = tableService;
        _bookingService = bookingService;
    }

    public async Task<IReadOnlyList<RestaurantTable>> GetAvailableTablesAsync(
        DateOnly date,
        TimeOnly time,
        CancellationToken ct = default)
    {
        var tables = await _tableService.GetAllTablesAsync(ct);
        var bookings = await _bookingService.GetByDateAsync(date, ct);

        var bookedTableIds = bookings
            .Where(b =>
                b.BookingTime == time &&
                b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Select(b => b.TableId)
            .ToList();

        return tables
            .Where(t => !bookedTableIds.Contains(t.TableId))
            .ToList();
    }

    public async Task<int> GetBookedTableCountAsync(
        DateOnly date,
        TimeOnly time,
        CancellationToken ct = default)
    {
        var bookings = await _bookingService.GetByDateAsync(date, ct);

        return bookings
            .Where(b =>
                b.BookingTime == time &&
                b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Select(b => b.TableId)
            .Distinct()
            .Count();
    }

    public async Task<int> GetAvailableTableCountAsync(
        DateOnly date,
        TimeOnly time,
        CancellationToken ct = default)
    {
        var availableTables = await GetAvailableTablesAsync(date, time, ct);
        return availableTables.Count;
    }

    public async Task<IEnumerable<Booking>> GetBookingsByDateAsync(
        DateOnly date,
        CancellationToken ct = default)
    {
        return await _bookingService.GetByDateAsync(date, ct);
    }

    public async Task<Dictionary<long, int>> GetTableBookingCountByDateTimeAsync(
        DateOnly date,
        TimeOnly time,
        CancellationToken ct = default)
    {
        var bookings = await _bookingService.GetByDateAsync(date, ct);

        var tableBookingCount = bookings
            .Where(b =>
                b.BookingTime == time &&
                b.BookingStatus != BookingStatusEnum.CANCELLED)
            .GroupBy(b => b.TableId)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        return tableBookingCount;
    }
}

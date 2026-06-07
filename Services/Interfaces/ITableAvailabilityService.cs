using BusinessObjects.Models;

namespace Services.Interfaces;

public interface ITableAvailabilityService
{
    /// <summary>
    /// Get available tables for a specific date and time
    /// </summary>
    Task<IReadOnlyList<RestaurantTable>> GetAvailableTablesAsync(
        DateOnly date,
        TimeOnly time,
        CancellationToken ct = default);

    /// <summary>
    /// Get booked table count for a specific date and time
    /// </summary>
    Task<int> GetBookedTableCountAsync(
        DateOnly date,
        TimeOnly time,
        CancellationToken ct = default);

    /// <summary>
    /// Get total available table count for a specific date and time
    /// </summary>
    Task<int> GetAvailableTableCountAsync(
        DateOnly date,
        TimeOnly time,
        CancellationToken ct = default);

    /// <summary>
    /// Get all bookings for a specific date with details
    /// </summary>
    Task<IEnumerable<Booking>> GetBookingsByDateAsync(
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>
    /// Get table with booking details for a specific date and time
    /// </summary>
    Task<Dictionary<long, int>> GetTableBookingCountByDateTimeAsync(
        DateOnly date,
        TimeOnly time,
        CancellationToken ct = default);
}

namespace Services.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Send SMS/Zalo notification to customer phone
    /// </summary>
    Task<bool> SendBookingConfirmationAsync(
        string phoneNumber,
        long bookingId,
        DateTime bookingDateTime,
        string guestName,
        CancellationToken ct = default);

    /// <summary>
    /// Send notification to customer about booking status change
    /// </summary>
    Task<bool> SendBookingStatusChangeAsync(
        string phoneNumber,
        long bookingId,
        string status,
        string message,
        CancellationToken ct = default);

    /// <summary>
    /// Send reminder notification before booking time
    /// </summary>
    Task<bool> SendBookingReminderAsync(
        string phoneNumber,
        long bookingId,
        DateTime bookingDateTime,
        string guestName,
        CancellationToken ct = default);
}

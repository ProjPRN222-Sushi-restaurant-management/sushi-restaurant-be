using BusinessObjects.Enums;
using Services.Interfaces;

namespace Services.Implementations;

/// <summary>
/// Background service that periodically updates table status and sends reminders
/// Runs every 5 minutes to check for:
/// 1. Expired bookings (update table status from BOOKED to AVAILABLE)
/// 2. Upcoming bookings (send reminder notifications)
/// </summary>
public class TableStatusUpdateHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TableStatusUpdateHostedService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(5);

    public TableStatusUpdateHostedService(
        IServiceProvider serviceProvider,
        ILogger<TableStatusUpdateHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Table Status Update Service is starting.");

        using var timer = new PeriodicTimer(_updateInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await UpdateTableStatusesAsync(stoppingToken);
                await SendRemindersAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Table Status Update Service is stopping.");
        }
        finally
        {
            timer.Dispose();
        }
    }

    private async Task UpdateTableStatusesAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var tableService = scope.ServiceProvider.GetRequiredService<IRestaurantTableService>();

            var allBookings = await bookingService.GetAllBookingsAsync(stoppingToken);

            var now = DateTime.Now;

            // Get bookings that have passed their booking time and are still PENDING or COMPLETED
            var expiredBookings = allBookings
                .Where(b =>
                    b.BookingDate < DateOnly.FromDateTime(now) ||
                    (b.BookingDate == DateOnly.FromDateTime(now) &&
                     b.BookingTime < TimeOnly.FromDateTime(now)) &&
                    (b.BookingStatus == BookingStatusEnum.PENDING ||
                     b.BookingStatus == BookingStatusEnum.COMPLETED))
                .ToList();

            foreach (var booking in expiredBookings)
            {
                // Update booking status to COMPLETED if it's PENDING
                if (booking.BookingStatus == BookingStatusEnum.PENDING)
                {
                    booking.BookingStatus = BookingStatusEnum.COMPLETED;
                    await bookingService.UpdateAsync(booking, stoppingToken);
                }
            }

            if (expiredBookings.Any())
            {
                await bookingService.SaveChangesAsync(stoppingToken);
                _logger.LogInformation(
                    $"Updated {expiredBookings.Count} expired bookings at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating table statuses");
        }
    }

    private async Task SendRemindersAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var allBookings = await bookingService.GetAllBookingsAsync(stoppingToken);

            var now = DateTime.Now;
            var nowDate = DateOnly.FromDateTime(now);
            var nowTime = TimeOnly.FromDateTime(now);

            // Get bookings that are coming up in the next 30 minutes
            var upcomingBookings = allBookings
                .Where(b =>
                    b.BookingDate == nowDate &&
                    b.BookingStatus == BookingStatusEnum.PENDING &&
                    b.BookingTime > nowTime &&
                    (b.BookingTime - nowTime).TotalMinutes <= 30)
                .ToList();

            foreach (var booking in upcomingBookings)
            {
                var bookingDateTime = booking.BookingDate.ToDateTime(booking.BookingTime);

                await notificationService.SendBookingReminderAsync(
                    booking.GuestPhone,
                    booking.BookingId,
                    bookingDateTime,
                    booking.GuestName,
                    stoppingToken);
            }

            if (upcomingBookings.Any())
            {
                _logger.LogInformation(
                    $"Sent {upcomingBookings.Count} reminder notifications at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending booking reminders");
        }
    }
}

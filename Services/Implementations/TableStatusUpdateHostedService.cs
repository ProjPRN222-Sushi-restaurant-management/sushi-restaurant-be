using BusinessObjects.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Policies;

namespace Services.Implementations;

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
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var tableService = scope.ServiceProvider.GetRequiredService<IRestaurantTableService>();

            var allBookings = await bookingService.GetAllBookingsAsync(stoppingToken);

            var now = DateTime.Now;
            var nowDate = DateOnly.FromDateTime(now);
            var nowTime = TimeOnly.FromDateTime(now);

            var expiredBookings = allBookings
                .Where(b =>
                    (b.BookingStatus == BookingStatusEnum.PENDING ||
                     b.BookingStatus == BookingStatusEnum.PREPARING) &&
                    (b.BookingDate < nowDate ||
                     (b.BookingDate == nowDate &&
                      b.BookingTime < nowTime)))
                .ToList();

            foreach (var booking in expiredBookings)
            {
                booking.BookingStatus = BookingStatusEnum.COMPLETED;
                await bookingService.UpdateAsync(booking, stoppingToken);
                await tableService.UpdateTableStatusAsync(
                    booking.TableId,
                    TableStatusEnum.AVAILABLE,
                    stoppingToken);
            }

            if (expiredBookings.Any())
            {
                await bookingService.SaveChangesAsync(stoppingToken);
                _logger.LogInformation(
                    $"Updated {expiredBookings.Count} expired bookings at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }

            var activeFutureBookings = allBookings
                .Where(b =>
                    b.BookingStatus == BookingStatusEnum.PENDING ||
                    b.BookingStatus == BookingStatusEnum.PREPARING)
                .Where(b =>
                    b.BookingDate > nowDate ||
                    (b.BookingDate == nowDate &&
                     b.BookingTime > nowTime))
                .ToList();

            var normalizedCount = 0;

            foreach (var booking in activeFutureBookings)
            {
                var targetStatus = BookingStatusPolicy.GetActiveStatus(booking, now);

                if (booking.BookingStatus != targetStatus)
                {
                    booking.BookingStatus = targetStatus;
                    await bookingService.UpdateAsync(booking, stoppingToken);
                    normalizedCount++;
                }

                await tableService.UpdateTableStatusAsync(
                    booking.TableId,
                    BookingStatusPolicy.ToTableStatus(targetStatus),
                    stoppingToken);

                await SyncActiveOrderStatusesAsync(
                    orderService,
                    booking.BookingId,
                    targetStatus);
            }

            if (normalizedCount > 0)
            {
                await bookingService.SaveChangesAsync(stoppingToken);
                _logger.LogInformation(
                    $"Normalized {normalizedCount} booking statuses at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
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

    private static async Task SyncActiveOrderStatusesAsync(
        IOrderService orderService,
        long bookingId,
        BookingStatusEnum bookingStatus)
    {
        var orderStatus = BookingStatusPolicy.ToOrderStatus(bookingStatus);
        var orders = await orderService.GetOrdersByBookingIdAsync(bookingId);

        foreach (var order in orders)
        {
            if (order.OrderStatus != OrderStatusEnum.PENDING &&
                order.OrderStatus != OrderStatusEnum.PREPARING)
            {
                continue;
            }

            if (order.OrderStatus == orderStatus)
            {
                continue;
            }

            order.OrderStatus = orderStatus;
            await orderService.UpdateOrderAsync(order);
        }
    }
}

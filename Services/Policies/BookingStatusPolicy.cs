using BusinessObjects.Enums;
using BusinessObjects.Models;

namespace Services.Policies;

public static class BookingStatusPolicy
{
    public static readonly TimeSpan PreparingWindow = TimeSpan.FromHours(6);

    public static BookingStatusEnum GetActiveStatus(Booking booking, DateTime now)
    {
        return GetActiveStatus(
            booking.BookingDate,
            booking.BookingTime,
            now);
    }

    public static BookingStatusEnum GetActiveStatus(
        DateOnly bookingDate,
        TimeOnly bookingTime,
        DateTime now)
    {
        var bookingDateTime = bookingDate.ToDateTime(bookingTime);
        var timeUntilBooking = bookingDateTime - now;

        return timeUntilBooking > TimeSpan.Zero &&
               timeUntilBooking < PreparingWindow
            ? BookingStatusEnum.PREPARING
            : BookingStatusEnum.PENDING;
    }

    public static OrderStatusEnum ToOrderStatus(BookingStatusEnum bookingStatus)
    {
        return bookingStatus switch
        {
            BookingStatusEnum.PREPARING => OrderStatusEnum.PREPARING,
            BookingStatusEnum.COMPLETED => OrderStatusEnum.COMPLETED,
            BookingStatusEnum.CANCELLED => OrderStatusEnum.CANCELLED,
            _ => OrderStatusEnum.PENDING
        };
    }
}

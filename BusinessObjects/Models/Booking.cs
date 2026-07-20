using BusinessObjects.Enums;

namespace BusinessObjects.Models;

public partial class Booking
{
    public long BookingId { get; set; }

    public long? CustomerId { get; set; }

    public long TableId { get; set; }

    public string GuestName { get; set; } = null!;

    public string GuestPhone { get; set; } = null!;

    public DateOnly BookingDate { get; set; }

    public TimeOnly BookingTime { get; set; }

    // Thời lượng dùng bữa (phút). Bàn bị chiếm trong [BookingTime, BookingTime + DurationMinutes).
    public int DurationMinutes { get; set; } = 90;

    public int GuestCount { get; set; }

    public BookingStatusEnum BookingStatus { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual RestaurantTable Table { get; set; } = null!;
}

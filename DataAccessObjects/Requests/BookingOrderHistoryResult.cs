using BusinessObjects.Models;

namespace DataAccessObjects.Requests;

public class BookingOrderHistoryResult
{
    public Booking Booking { get; set; } = null!;

    public IEnumerable<Order> Orders { get; set; } = new List<Order>();

    public decimal TemporaryTotal { get; set; }
}
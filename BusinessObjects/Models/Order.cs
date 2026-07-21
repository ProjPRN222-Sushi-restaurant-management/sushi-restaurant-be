using BusinessObjects.Enums;

namespace BusinessObjects.Models;

public partial class Order
{
    public long OrderId { get; set; }

    public long? BookingId { get; set; }

    public long? CustomerId { get; set; }

    public long? TableId { get; set; }

    public OrderStatusEnum OrderStatus { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
    public long? ReceivedStaffId { get; set; }

    public string? ReceivedStaffName { get; set; }

    public DateTime? ReceivedAt { get; set; }
    public long? InvoiceStaffId { get; set; }

    public string? InvoiceStaffName { get; set; }

    public DateTime? InvoiceIssuedAt { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Staff? InvoiceStaff { get; set; }
    public virtual Staff? ReceivedStaff { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual RestaurantTable? Table { get; set; }
}

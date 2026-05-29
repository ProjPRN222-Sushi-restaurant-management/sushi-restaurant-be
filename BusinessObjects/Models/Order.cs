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

    public virtual Booking? Booking { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual RestaurantTable? Table { get; set; }
}

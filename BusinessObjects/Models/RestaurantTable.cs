using BusinessObjects.Enums;

namespace BusinessObjects.Models;

public partial class RestaurantTable
{
    public long TableId { get; set; }

    public string TableNum { get; set; } = null!;

    public TableTypeEnum TableType { get; set; }

    public int Capacity { get; set; }

    public TableStatusEnum TableStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

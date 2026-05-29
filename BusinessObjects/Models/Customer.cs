using BusinessObjects.Enums;
using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Customer
{
    public long CustomerId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public MembershipLevelEnum MembershipLevel { get; set; }

    public int LoyaltyPoints { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

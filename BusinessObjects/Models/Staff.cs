using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class Staff
{
    public long StaffId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}

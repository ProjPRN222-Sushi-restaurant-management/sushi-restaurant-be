using System;
using System.Collections.Generic;

namespace BusinessObjects.Models;

public partial class MenuCategory
{
    public long CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}

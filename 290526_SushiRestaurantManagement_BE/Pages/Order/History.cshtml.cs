using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Order;

public class HistoryModel : PageModel
{
    private readonly IBookingService _bookingService;
    private readonly IOrderService _orderService;

    public HistoryModel(
        IBookingService bookingService,
        IOrderService orderService)
    {
        _bookingService = bookingService;
        _orderService = orderService;
    }

    public decimal TemporaryTotal { get; set; }
    public BusinessObjects.Models.Booking? Booking { get; set; }

    public IEnumerable<BusinessObjects.Models.Order> Orders { get; set; }
        = new List<BusinessObjects.Models.Order>();

    public async Task<IActionResult> OnGetAsync(long bookingId)
    {
        Booking = await _bookingService.GetBookingByIdAsync(bookingId);

        if (Booking == null)
        {
            return NotFound();
        }

        Orders = await _orderService.GetOrdersByBookingIdAsync(bookingId);
        TemporaryTotal = Orders
            .SelectMany(o => o.OrderItems)
            .Sum(d => d.Quantity * d.UnitPrice);

        return Page();
    }
}
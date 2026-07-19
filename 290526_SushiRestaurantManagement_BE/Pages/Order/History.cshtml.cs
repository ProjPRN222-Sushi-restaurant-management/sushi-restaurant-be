using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _290526_SushiRestaurantManagement_BE.Pages.Order;

public class HistoryModel : PageModel
{
    private readonly IBookingService _bookingService;

    public HistoryModel(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    public decimal TemporaryTotal { get; set; }

    public BusinessObjects.Models.Booking? Booking { get; set; }

    public IEnumerable<BusinessObjects.Models.Order> Orders { get; set; }
        = new List<BusinessObjects.Models.Order>();

    public async Task<IActionResult> OnGetAsync(long bookingId)
    {
        try
        {
            var result = await _bookingService.GetBookingOrderHistoryAsync(bookingId);

            Booking = result.Booking;
            Orders = result.Orders;
            TemporaryTotal = result.TemporaryTotal;

            return Page();
        }
        catch
        {
            return NotFound();
        }
    }
}
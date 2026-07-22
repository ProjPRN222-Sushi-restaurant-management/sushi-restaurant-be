using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _290526_SushiRestaurantManagement_BE.Pages.Booking;

public class AvailabilityModel : PageModel
{
    public IActionResult OnGet(DateOnly? selectedDate)
    {
        return RedirectToPage("/Booking/Reservations", new
        {
            selectedDate = selectedDate?.ToString("yyyy-MM-dd")
        });
    }
}

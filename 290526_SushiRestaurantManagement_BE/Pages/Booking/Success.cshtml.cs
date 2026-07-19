using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _290526_SushiRestaurantManagement_BE.Pages.Booking
{
    public class SuccessModel : PageModel
    {
        public long BookingId { get; set; }
        public void OnGet(long id)
        {
            BookingId = id;
        }
    }
}

using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Booking;

public class AvailabilityModel : PageModel
{
    private readonly ITableAvailabilityService _tableAvailabilityService;
    private readonly IRestaurantTableService _tableService;
    private readonly IBookingService _bookingService;

    public AvailabilityModel(
        ITableAvailabilityService tableAvailabilityService,
        IRestaurantTableService tableService,
        IBookingService bookingService)
    {
        _tableAvailabilityService = tableAvailabilityService;
        _tableService = tableService;
        _bookingService = bookingService;
    }

    public DateOnly SelectedDate { get; set; }
    public int TotalTables { get; set; }
    public int BookedTablesCount { get; set; }
    public int AvailableTablesCount { get; set; }
    public decimal OccupancyRate { get; set; }
    public PaginatedList<BusinessObjects.Models.Booking> Bookings { get; set; } = new(new List<BusinessObjects.Models.Booking>(), 0, 1, 10);
    
    public List<TimeSlotInfo> TimeSlots { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public async Task OnGetAsync(DateOnly? selectedDate)
    {
        SelectedDate = selectedDate ?? DateOnly.FromDateTime(DateTime.Today);

        // Load all tables
        var allTables = await _tableService.GetAllTablesAsync();
        TotalTables = allTables.Count;

        // Load bookings for selected date
        var allBookings = (await _bookingService.GetByDateAsync(SelectedDate)).ToList();
        Bookings = PaginatedList<BusinessObjects.Models.Booking>.Create(allBookings, PageNumber, PageSize);

        // Initialize time slots (typical restaurant hours: 11:00 - 22:00)
        var timeSlots = new List<TimeOnly>();
        for (int hour = 11; hour < 23; hour++)
        {
            timeSlots.Add(new TimeOnly(hour, 0));
            timeSlots.Add(new TimeOnly(hour, 30));
        }

        // Calculate occupancy for each time slot
        foreach (var timeSlot in timeSlots)
        {
            var bookedCount = await _tableAvailabilityService.GetBookedTableCountAsync(
                SelectedDate, timeSlot);
            var availableCount = TotalTables - bookedCount;
            var occupancyPercent = TotalTables > 0
                ? Math.Round((decimal)bookedCount / TotalTables * 100)
                : 0;

            TimeSlots.Add(new TimeSlotInfo
            {
                Time = timeSlot,
                BookedCount = bookedCount,
                AvailableCount = availableCount,
                OccupancyPercentage = occupancyPercent
            });
        }

        // Calculate overall occupancy for the day
        var totalBookingsToday = Bookings.Items.Count;
        BookedTablesCount = Bookings
            .Items
            .Select(b => b.TableId)
            .Distinct()
            .Count();
        AvailableTablesCount = TotalTables - BookedTablesCount;
        OccupancyRate = TotalTables > 0
            ? Math.Round((decimal)BookedTablesCount / TotalTables * 100)
            : 0;
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(
    long bookingId,
    BookingStatusEnum status,
    DateOnly selectedDate)
    {
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);

        if (booking == null)
        {
            return NotFound();
        }

        booking.BookingStatus = status;

        await _bookingService.UpdateAsync(booking);
        await _bookingService.SaveChangesAsync();

        return RedirectToPage("/Booking/Availability", new
        {
            selectedDate = selectedDate.ToString("yyyy-MM-dd")
        });
    }

    public class TimeSlotInfo
    {
        public TimeOnly Time { get; set; }
        public int BookedCount { get; set; }
        public int AvailableCount { get; set; }
        public decimal OccupancyPercentage { get; set; }
    }
}

//using BusinessObjects.Enums;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;

//namespace _290526_SushiRestaurantManagement_BE.Pages.Booking;

//public class AvailabilityModel : PageModel
//{
//    private readonly IBookingService _bookingService;

//    public AvailabilityModel(
//        IBookingService bookingService)
//    {
//        _bookingService = bookingService;
//    }

//    public async Task<IActionResult> OnPostUpdateStatusAsync(
//        long bookingId,
//        BookingStatusEnum status,
//        DateOnly selectedDate)
//    {
//        await _bookingService.UpdateBookingStatusAsync(bookingId, status);

//        return RedirectToPage("/Booking/Availability", new
//        {
//            selectedDate = selectedDate.ToString("yyyy-MM-dd")
//        });
//    }
//}
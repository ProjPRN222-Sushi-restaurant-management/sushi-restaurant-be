using BusinessObjects.Models;
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
    public IEnumerable<Booking> Bookings { get; set; } = [];
    public List<TimeSlotInfo> TimeSlots { get; set; } = [];

    public async Task OnGetAsync(DateOnly? selectedDate)
    {
        SelectedDate = selectedDate ?? DateOnly.FromDateTime(DateTime.Today);

        // Load all tables
        var allTables = await _tableService.GetAllTablesAsync();
        TotalTables = allTables.Count;

        // Load bookings for selected date
        Bookings = (await _bookingService.GetByDateAsync(SelectedDate)).ToList();

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
        var totalBookingsToday = Bookings.Count();
        BookedTablesCount = Bookings
            .Select(b => b.TableId)
            .Distinct()
            .Count();
        AvailableTablesCount = TotalTables - BookedTablesCount;
        OccupancyRate = TotalTables > 0 
            ? Math.Round((decimal)BookedTablesCount / TotalTables * 100)
            : 0;
    }

    public class TimeSlotInfo
    {
        public TimeOnly Time { get; set; }
        public int BookedCount { get; set; }
        public int AvailableCount { get; set; }
        public decimal OccupancyPercentage { get; set; }
    }
}

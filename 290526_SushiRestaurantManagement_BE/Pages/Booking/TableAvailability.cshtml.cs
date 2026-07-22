using BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Booking;

public class TableAvailabilityModel : PageModel
{
    private readonly ITableAvailabilityService _tableAvailabilityService;
    private readonly IRestaurantTableService _tableService;
    private readonly IBookingService _bookingService;

    public TableAvailabilityModel(
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

    public List<TimeSlotInfo> TimeSlots { get; set; } = [];

    public List<AreaStat> AreaStats { get; set; } = [];

    public async Task OnGetAsync(DateOnly? selectedDate)
    {
        SelectedDate = selectedDate ?? DateOnly.FromDateTime(DateTime.Today);

        var allTables = await _tableService.GetAllTablesAsync();
        var allBookings = (await _bookingService.GetByDateAsync(SelectedDate)).ToList();

        TotalTables = allTables.Count;

        var timeSlots = new List<TimeOnly>();
        for (var hour = 11; hour < 23; hour++)
        {
            timeSlots.Add(new TimeOnly(hour, 0));
            timeSlots.Add(new TimeOnly(hour, 30));
        }

        foreach (var timeSlot in timeSlots)
        {
            var bookedCount = await _tableAvailabilityService.GetBookedTableCountAsync(
                SelectedDate,
                timeSlot);

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

        var bookedTableIds = allBookings
            .Where(b => b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Select(b => b.TableId)
            .Distinct()
            .ToHashSet();

        BookedTablesCount = bookedTableIds.Count;
        AvailableTablesCount = TotalTables - BookedTablesCount;
        OccupancyRate = TotalTables > 0
            ? Math.Round((decimal)BookedTablesCount / TotalTables * 100)
            : 0;

        AreaStats = allTables
            .GroupBy(t => t.TableType)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var free = g.Where(t => !bookedTableIds.Contains(t.TableId)).ToList();

                return new AreaStat
                {
                    AreaName = GetAreaName(g.Key),
                    TableCount = g.Count(),
                    TotalSeats = g.Sum(t => t.Capacity),
                    BookedTables = g.Count(t => bookedTableIds.Contains(t.TableId)),
                    AvailableTables = free.Count,
                    AvailableSeats = free.Sum(t => t.Capacity)
                };
            })
            .ToList();
    }

    private static string GetAreaName(TableTypeEnum type) => type switch
    {
        TableTypeEnum.NORMAL => "Bàn thường",
        TableTypeEnum.VIP => "VIP",
        TableTypeEnum.BAR => "Quầy bar",
        _ => type.ToString()
    };

    public class TimeSlotInfo
    {
        public TimeOnly Time { get; set; }

        public int BookedCount { get; set; }

        public int AvailableCount { get; set; }

        public decimal OccupancyPercentage { get; set; }
    }

    public class AreaStat
    {
        public string AreaName { get; set; } = "";

        public int TableCount { get; set; }

        public int TotalSeats { get; set; }

        public int BookedTables { get; set; }

        public int AvailableTables { get; set; }

        public int AvailableSeats { get; set; }
    }
}

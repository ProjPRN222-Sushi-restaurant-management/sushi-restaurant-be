using System.Globalization;
using System.Text.Json;
using BusinessObjects.Enums;
using BusinessObjects.Models;
using DataAccessObjects;
using DataAccessObjects.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Booking;

public class CreateModel : PageModel
{
    private readonly IBookingService _bookingService;
    private readonly IRestaurantTableService _tableService;
    private readonly INotificationService _notificationService;
    private readonly RestaurantSystemDbContext _context;

    public CreateModel(
        IBookingService bookingService,
        IRestaurantTableService tableService,
        INotificationService notificationService,
        RestaurantSystemDbContext context)
    {
        _bookingService = bookingService;
        _tableService = tableService;
        _notificationService = notificationService;
        _context = context;
    }

    [BindProperty]
    public BookingInput Input { get; set; } = new();

    public List<RestaurantTable> AvailableTables { get; set; } = [];

    public List<BusinessObjects.Models.Booking> ExistingBookings { get; set; } = [];

    public int AvailableTableCount { get; set; }

    public List<AreaSeatInfo> AreaSeatInfos { get; set; } = [];

    public List<AreaTableStatus> TableStatusByArea { get; set; } = [];

    public string? UnavailableReason { get; set; }

    public int SelectedAreaFreeCount { get; set; }

    public string AvailabilityJson { get; set; } = "{}";

    public async Task OnGetAsync()
    {
        Input.BookingDate = DateOnly.FromDateTime(DateTime.Today);
        Input.BookingTime = new TimeOnly(18, 0);
        await LoadAvailableTablesAsync();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await LoadAvailableTablesAsync();
            return Page();
        }

        var nowMinute = DateTime.Now;
        nowMinute = nowMinute.AddSeconds(-nowMinute.Second).AddMilliseconds(-nowMinute.Millisecond);
        if (Input.BookingDate.ToDateTime(Input.BookingTime) < nowMinute)
        {
            await LoadAvailableTablesAsync();
            UnavailableReason = "Không thể đặt bàn cho thời điểm đã qua. Vui lòng chọn ngày/giờ từ hiện tại trở đi.";
            ModelState.AddModelError(string.Empty, UnavailableReason);
            return Page();
        }

        try
        {
            var request = new CreateBookingRequest
            {
                BookingDate = Input.BookingDate,
                BookingTime = Input.BookingTime,
                DurationMinutes = Input.DurationMinutes,
                TableId = Input.TableId,
                AdultCount = Input.AdultCount,
                ChildCount = Input.ChildCount,
                TableType = Input.TableType,
                GuestName = Input.GuestName,
                GuestPhone = Input.GuestPhone,
                Note = Input.Note
            };

            var booking = await _bookingService.CreateBookingAsync(request, ct);

            TempData["BookingSuccess"] = $"Đặt bàn thành công! Mã đặt bàn của bạn là #{booking.BookingId}.";
            return RedirectToPage("/Booking/Success", new { id = booking.BookingId });
        }
        catch (InvalidOperationException ex)
        {
            await LoadAvailableTablesAsync();
            ModelState.AddModelError(string.Empty, UnavailableReason ?? ex.Message);
            return Page();
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Đã có lỗi hệ thống xảy ra. Vui lòng thử lại sau.");
            await LoadAvailableTablesAsync();
            return Page();
        }
    }

    private async Task<List<RestaurantTable>> GetAvailableTablesAsync()
    {
        var tables = await _tableService.GetAllTablesAsync();
        var bookedTables = await _bookingService.GetByDateAsync(Input.BookingDate);
        var reqStart = ToMinutes(Input.BookingTime);
        var reqEnd = reqStart + (Input.DurationMinutes > 0 ? Input.DurationMinutes : 90);

        var bookedTableIds = bookedTables
            .Where(b => b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Where(b => Overlaps(reqStart, reqEnd, b))
            .Select(b => b.TableId)
            .ToHashSet();

        return tables
            .Where(t =>
                t.TableType == Input.TableType &&
                t.Capacity >= Input.AdultCount + Input.ChildCount &&
                !bookedTableIds.Contains(t.TableId))
            .OrderBy(t => t.Capacity)
            .ToList();
    }

    private static bool Overlaps(int reqStart, int reqEnd, BusinessObjects.Models.Booking b)
    {
        var start = ToMinutes(b.BookingTime);
        var end = start + (b.DurationMinutes > 0 ? b.DurationMinutes : 90);
        return reqStart < end && start < reqEnd;
    }

    private static int ToMinutes(TimeOnly t) => (int)t.ToTimeSpan().TotalMinutes;

    private async Task LoadAvailableTablesAsync()
    {
        AvailableTables = await GetAvailableTablesAsync();
        AvailableTableCount = AvailableTables.Count;

        var existStart = ToMinutes(Input.BookingTime);
        var existEnd = existStart + (Input.DurationMinutes > 0 ? Input.DurationMinutes : 90);
        ExistingBookings = (await _bookingService.GetByDateAsync(Input.BookingDate))
            .Where(b => b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Where(b => Overlaps(existStart, existEnd, b))
            .ToList();

        var allTables = await _tableService.GetAllTablesAsync();
        AreaSeatInfos = allTables
            .GroupBy(t => t.TableType)
            .Select(g => new AreaSeatInfo
            {
                TableType = g.Key,
                TableCount = g.Count(),
                TotalSeats = g.Sum(t => t.Capacity),
                MinCapacity = g.Min(t => t.Capacity),
                MaxCapacity = g.Max(t => t.Capacity),
            })
            .ToList();

        var busyStart = ToMinutes(Input.BookingTime);
        var busyEnd = busyStart + (Input.DurationMinutes > 0 ? Input.DurationMinutes : 90);
        var bookedTableIds = (await _bookingService.GetByDateAsync(Input.BookingDate))
            .Where(b => b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Where(b => Overlaps(busyStart, busyEnd, b))
            .Select(b => b.TableId)
            .ToHashSet();

        TableStatusByArea = allTables
            .GroupBy(t => t.TableType)
            .OrderBy(g => g.Key)
            .Select(g => new AreaTableStatus
            {
                AreaName = GetAreaName(g.Key),
                AreaType = g.Key,
                Tables = g.OrderBy(t => t.TableNum).Select(t => new TableStatusView
                {
                    TableId = t.TableId,
                    TableNum = t.TableNum,
                    Capacity = t.Capacity,
                    IsBooked = bookedTableIds.Contains(t.TableId)
                }).ToList()
            })
            .ToList();

        SelectedAreaFreeCount = allTables
            .Count(t => t.TableType == Input.TableType && !bookedTableIds.Contains(t.TableId));
        AvailabilityJson = JsonSerializer.Serialize(BuildAvailabilityPayload(allTables, bookedTableIds));

        UnavailableReason = AvailableTables.Count == 0
            ? BuildUnavailableReason(allTables)
            : null;
    }

    private string BuildUnavailableReason(IReadOnlyList<RestaurantTable> allTables)
    {
        var guestCount = Input.AdultCount + Input.ChildCount;
        var areaName = GetAreaName(Input.TableType);
        var areaTables = allTables.Where(t => t.TableType == Input.TableType).ToList();

        if (areaTables.Count == 0)
        {
            return $"Khu vực {areaName} hiện chưa có bàn nào.";
        }

        var maxCapacity = areaTables.Max(t => t.Capacity);
        if (guestCount > maxCapacity)
        {
            return $"Số khách ({guestCount}) vượt quá sức chứa mỗi bàn của khu vực {areaName} " +
                   $"(tối đa {maxCapacity} khách/bàn). Vui lòng chọn khu vực khác, giảm số khách hoặc tách thành nhiều bàn.";
        }

        return $"Khu vực {areaName} đã kín bàn phù hợp vào {Input.BookingTime.ToString("HH:mm")} " +
               $"ngày {Input.BookingDate.ToString("dd/MM/yyyy")}. Vui lòng chọn khung giờ hoặc ngày khác.";
    }

    private static string GetAreaName(TableTypeEnum type) => type switch
    {
        TableTypeEnum.NORMAL => "Bàn thường",
        TableTypeEnum.VIP => "VIP",
        TableTypeEnum.BAR => "Quầy bar",
        _ => type.ToString()
    };

    private object BuildAvailabilityPayload(IReadOnlyList<RestaurantTable> allTables, HashSet<long> bookedTableIds) =>
        new
        {
            areas = allTables
                .GroupBy(t => t.TableType)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    type = g.Key.ToString(),
                    name = GetAreaName(g.Key),
                    tables = g.OrderBy(t => t.TableNum).Select(t => new
                    {
                        id = t.TableId,
                        num = t.TableNum,
                        cap = t.Capacity,
                        booked = bookedTableIds.Contains(t.TableId)
                    }).ToList()
                })
                .ToList()
        };

    public async Task<IActionResult> OnGetAvailabilityAsync(string? date, string? time, int duration, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            d = DateOnly.FromDateTime(DateTime.Today);
        }

        if (!TimeOnly.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tm))
        {
            tm = new TimeOnly(18, 0);
        }

        if (duration <= 0)
        {
            duration = 90;
        }

        var allTables = await _tableService.GetAllTablesAsync(ct);
        var reqStart = ToMinutes(tm);
        var reqEnd = reqStart + duration;

        var bookedTableIds = (await _bookingService.GetByDateAsync(d, ct))
            .Where(b => b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Where(b => Overlaps(reqStart, reqEnd, b))
            .Select(b => b.TableId)
            .ToHashSet();

        return new JsonResult(BuildAvailabilityPayload(allTables, bookedTableIds));
    }
}

public class BookingInput
{
    public DateOnly BookingDate { get; set; }

    public TimeOnly BookingTime { get; set; }

    public int DurationMinutes { get; set; } = 90;

    public long? TableId { get; set; }

    public int AdultCount { get; set; } = 2;

    public int ChildCount { get; set; } = 0;

    public TableTypeEnum TableType { get; set; }

    public string GuestName { get; set; } = "";

    public string GuestPhone { get; set; } = "";

    public string? Note { get; set; }
}

public class AreaSeatInfo
{
    public BusinessObjects.Enums.TableTypeEnum TableType { get; set; }

    public int TableCount { get; set; }

    public int TotalSeats { get; set; }

    public int MinCapacity { get; set; }

    public int MaxCapacity { get; set; }
}

public class AreaTableStatus
{
    public string AreaName { get; set; } = "";

    public BusinessObjects.Enums.TableTypeEnum AreaType { get; set; }

    public List<TableStatusView> Tables { get; set; } = [];
}

public class TableStatusView
{
    public long TableId { get; set; }

    public string TableNum { get; set; } = "";

    public int Capacity { get; set; }

    public bool IsBooked { get; set; }
}

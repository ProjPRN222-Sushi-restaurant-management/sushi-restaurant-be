using System.Globalization;
using System.Text.Json;
using BusinessObjects.Enums;
using BusinessObjects.Models;
using DataAccessObjects;
using DataAccessObjects.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

    // Thông tin số bàn / số ghế theo từng khu vực (để hiển thị khi chọn khu vực)
    public List<AreaSeatInfo> AreaSeatInfos { get; set; } = [];

    // Trạng thái tất cả bàn theo khu vực (đã đặt / còn trống) tại khung giờ đang chọn
    public List<AreaTableStatus> TableStatusByArea { get; set; } = [];

    // Lý do cụ thể khi không còn bàn phù hợp (do sức chứa hay do khung giờ)
    public string? UnavailableReason { get; set; }

    // Số bàn còn trống ở khu vực đang chọn (theo khung giờ) - hiển thị ở ô "Bàn phù hợp"
    public int SelectedAreaFreeCount { get; set; }

    // JSON trạng thái tất cả bàn (theo khung giờ) để JS cập nhật realtime không cần load lại trang
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

        // Chặn đặt bàn cho thời điểm đã qua (server-side, không thể bị bỏ qua từ client)
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
            // Mapping d? li?u t? Form (BookingInput) sang DTO Request c?a Service
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

            // G?i Service x? l� tr?n g�i to�n b? nghi?p v?
            var booking = await _bookingService.CreateBookingAsync(request, ct);

            TempData["BookingSuccess"] = $"??t b�n th�nh c�ng! M� booking c?a b?n l� #{booking.BookingId}.";
            return RedirectToPage("/Booking/Success", new { id = booking.BookingId });
        }
        catch (InvalidOperationException ex) // B?t ?�ng l?i nghi?p v? (h?t b�n, sai d? li?u...)
        {
            await LoadAvailableTablesAsync(); // N?p l?i danh s�ch + t�nh l� do c? th?
            ModelState.AddModelError("", UnavailableReason ?? ex.Message);
            return Page();
        }
        catch (Exception) // B?t c�c l?i h? th?ng kh�ng l??ng tr??c ???c
        {
            ModelState.AddModelError("", "?� c� l?i h? th?ng x?y ra. Vui l�ng th? l?i sau.");
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

        // Bàn bị chiếm nếu có booking (chưa hủy) TRÙNG KHOẢNG thời gian đang chọn
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
        var s = ToMinutes(b.BookingTime);
        var e = s + (b.DurationMinutes > 0 ? b.DurationMinutes : 90);
        return reqStart < e && s < reqEnd;
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

        // Tổng hợp số bàn / số ghế theo khu vực
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

        // Trạng thái tất cả bàn theo khu vực tại khung giờ đang chọn (để chọn bàn + theo dõi)
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

        // Số bàn trống ở khu vực đang chọn + JSON để JS cập nhật realtime
        SelectedAreaFreeCount = allTables
            .Count(t => t.TableType == Input.TableType && !bookedTableIds.Contains(t.TableId));
        AvailabilityJson = JsonSerializer.Serialize(BuildAvailabilityPayload(allTables, bookedTableIds));

        // Không còn bàn phù hợp -> xác định lý do cụ thể
        UnavailableReason = AvailableTables.Count == 0
            ? BuildUnavailableReason(allTables)
            : null;
    }

    // Phân biệt nguyên nhân: quá sức chứa khu vực hay khung giờ đã kín
    private string BuildUnavailableReason(IReadOnlyList<RestaurantTable> allTables)
    {
        var guestCount = Input.AdultCount + Input.ChildCount;
        var areaName = GetAreaName(Input.TableType);
        var areaTables = allTables.Where(t => t.TableType == Input.TableType).ToList();

        if (areaTables.Count == 0)
            return $"Khu vực {areaName} hiện chưa có bàn nào.";

        var maxCapacity = areaTables.Max(t => t.Capacity);

        // Không bàn nào trong khu vực đủ chỗ cho số khách -> lỗi SỨC CHỨA
        if (guestCount > maxCapacity)
            return $"Số khách ({guestCount}) vượt quá sức chứa mỗi bàn của khu vực {areaName} " +
                   $"(tối đa {maxCapacity} khách/bàn). Vui lòng chọn khu vực khác, giảm số khách hoặc tách thành nhiều bàn.";

        // Có bàn đủ chỗ nhưng đều đã được đặt ở khung giờ này -> lỗi KHUNG GIỜ
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

    private async Task<RestaurantTable?> FindAvailableTableAsync()
    {
        var availableTables = await GetAvailableTablesAsync();

        return availableTables.FirstOrDefault();
    }

    // Gói dữ liệu trạng thái tất cả bàn theo khu vực (dùng chung cho render lần đầu + AJAX)
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

    // AJAX: trả về trạng thái bàn theo ngày/giờ/thời lượng để cập nhật ngay khi đổi khung giờ
    public async Task<IActionResult> OnGetAvailabilityAsync(string? date, string? time, int duration, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            d = DateOnly.FromDateTime(DateTime.Today);
        if (!TimeOnly.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tm))
            tm = new TimeOnly(18, 0);
        if (duration <= 0) duration = 90;

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

    // Bàn nhân viên chủ động chọn (null = để hệ thống tự gán)
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


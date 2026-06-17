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

        try
        {
            // Mapping d? li?u t? Form (BookingInput) sang DTO Request c?a Service
            var request = new CreateBookingRequest
            {
                BookingDate = Input.BookingDate,
                BookingTime = Input.BookingTime,
                AdultCount = Input.AdultCount,
                ChildCount = Input.ChildCount,
                TableType = Input.TableType,
                GuestName = Input.GuestName,
                GuestPhone = Input.GuestPhone,
                Note = Input.Note
            };

            // G?i Service x? lý tr?n gói toàn b? nghi?p v?
            var booking = await _bookingService.CreateBookingAsync(request, ct);

            TempData["BookingSuccess"] = $"??t bàn thành công! Mã booking c?a b?n là #{booking.BookingId}.";
            return RedirectToPage("/Booking/Success", new { id = booking.BookingId });
        }
        catch (InvalidOperationException ex) // B?t ?úng l?i nghi?p v? (h?t bàn, sai d? li?u...)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadAvailableTablesAsync(); // N?p l?i danh sách ?? hi?n th? giao di?n
            return Page();
        }
        catch (Exception) // B?t các l?i h? th?ng không l??ng tr??c ???c
        {
            ModelState.AddModelError("", "?ã có l?i h? th?ng x?y ra. Vui lòng th? l?i sau.");
            await LoadAvailableTablesAsync();
            return Page();
        }
    }

    private async Task<List<RestaurantTable>> GetAvailableTablesAsync()
    {
        var tables = await _tableService.GetAllTablesAsync();

        var bookedTables = await _bookingService.GetByDateAsync(Input.BookingDate);

        var bookedTableIds = bookedTables
            .Where(b =>
                b.BookingTime == Input.BookingTime &&
                b.BookingStatus != BookingStatusEnum.CANCELLED)
            .Select(b => b.TableId)
            .ToList();

        return tables
            .Where(t =>
                t.TableType == Input.TableType &&
                t.Capacity >= Input.AdultCount + Input.ChildCount &&
                !bookedTableIds.Contains(t.TableId))
            .OrderBy(t => t.Capacity)
            .ToList();
    }

    private async Task LoadAvailableTablesAsync()
    {
        AvailableTables = await GetAvailableTablesAsync();
        AvailableTableCount = AvailableTables.Count;

        ExistingBookings = (await _bookingService.GetByDateAsync(Input.BookingDate))
            .Where(b =>
                b.BookingTime == Input.BookingTime &&
                b.BookingStatus != BookingStatusEnum.CANCELLED)
            .ToList();
    }

    private async Task<RestaurantTable?> FindAvailableTableAsync()
    {
        var availableTables = await GetAvailableTablesAsync();

        return availableTables.FirstOrDefault();
    }
}

public class BookingInput
{
    public DateOnly BookingDate { get; set; }

    public TimeOnly BookingTime { get; set; }

    public int AdultCount { get; set; } = 2;

    public int ChildCount { get; set; } = 0;

    public TableTypeEnum TableType { get; set; }

    public string GuestName { get; set; } = "";

    public string GuestPhone { get; set; } = "";

    public string? Note { get; set; }
}


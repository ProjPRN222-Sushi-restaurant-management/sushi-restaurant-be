using BusinessObjects.Enums;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Booking;

public class CreateModel : PageModel
{
    private readonly IBookingService _bookingService;
    private readonly IRestaurantTableService _tableService;

    public CreateModel(
        IBookingService bookingService,
        IRestaurantTableService tableService)
    {
        _bookingService = bookingService;
        _tableService = tableService;
    }

    [BindProperty]
    public BookingInput Input { get; set; } = new();

    public int AvailableTableCount { get; set; }

    public async Task OnGetAsync()
    {
        Input.BookingDate = DateOnly.FromDateTime(DateTime.Today);
        Input.BookingTime = new TimeOnly(18, 0);
        await LoadAvailableTablesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAvailableTablesAsync();
            return Page();
        }

        var table = await FindAvailableTableAsync();

        if (table == null)
        {
            ModelState.AddModelError("", "Không còn bàn tr?ng phù h?p trong khung gi? này.");
            await LoadAvailableTablesAsync();
            return Page();
        }

        var booking = new BusinessObjects.Models.Booking
        {
            GuestName = Input.GuestName,
            GuestPhone = Input.GuestPhone,
            BookingDate = Input.BookingDate,
            BookingTime = Input.BookingTime,
            GuestCount = Input.AdultCount + Input.ChildCount,
            TableId = table.TableId,
            BookingStatus = BookingStatusEnum.PENDING,
            Note = $"Ng??i l?n: {Input.AdultCount}, Tr? em: {Input.ChildCount}. {Input.Note}",
            CreatedAt = DateTime.Now
        };

        await _bookingService.AddAsync(booking);
        await _bookingService.SaveChangesAsync();

        TempData["BookingSuccess"] =
            $"??t bàn thành công! Mã booking c?a b?n là #{booking.BookingId}. Nhà hàng s? xác nh?n s?m.";

        return RedirectToPage("/Booking/Success", new { id = booking.BookingId });
    }

    private async Task LoadAvailableTablesAsync()
    {
        var table = await FindAvailableTableAsync();
        AvailableTableCount = table == null ? 0 : 1;
    }

    private async Task<RestaurantTable?> FindAvailableTableAsync()
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
            .FirstOrDefault();
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
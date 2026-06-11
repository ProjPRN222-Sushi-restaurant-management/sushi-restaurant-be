using BusinessObjects.Enums;
using BusinessObjects.Models;
using DataAccessObjects;
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
            ModelState.AddModelError("", "Khong con ban trong phu hop trong khung gio nay.");
            await LoadAvailableTablesAsync();
            return Page();
        }

        var phone = Input.GuestPhone.Trim();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Phone == phone);

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = Input.GuestName.Trim(),
                Phone = phone,
                CreatedAt = DateTime.Now
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            customer.FullName = Input.GuestName.Trim();
            await _context.SaveChangesAsync();
        }

        var booking = new BusinessObjects.Models.Booking
        {
            CustomerId = customer.CustomerId,
            GuestName = Input.GuestName,
            GuestPhone = Input.GuestPhone,
            BookingDate = Input.BookingDate,
            BookingTime = Input.BookingTime,
            GuestCount = Input.AdultCount + Input.ChildCount,
            TableId = table.TableId,
            BookingStatus = BookingStatusEnum.PENDING,
            Note = $"Nguoi lon: {Input.AdultCount}, Tre em: {Input.ChildCount}. {Input.Note}",
            CreatedAt = DateTime.Now
        };

        await _bookingService.AddAsync(booking);
        await _bookingService.SaveChangesAsync();

        var bookingDateTime = booking.BookingDate.ToDateTime(booking.BookingTime);

        await _notificationService.SendBookingConfirmationAsync(
            booking.GuestPhone,
            booking.BookingId,
            bookingDateTime,
            booking.GuestName
        );

        TempData["BookingSuccess"] =
            $"Dat ban thanh cong! Ma booking cua ban la #{booking.BookingId}. Nha hang se xac nhan som.";

        return RedirectToPage("/Booking/Success", new { id = booking.BookingId });
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
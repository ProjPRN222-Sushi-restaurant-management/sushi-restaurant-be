using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

using OrderEntity = BusinessObjects.Models.Order;

namespace _290526_SushiRestaurantManagement_BE.Pages.Booking;

public class AvailabilityModel : PageModel
{
    private const decimal AmountPerLoyaltyPoint = 10000m;

    private readonly ITableAvailabilityService _tableAvailabilityService;
    private readonly IRestaurantTableService _tableService;
    private readonly IBookingService _bookingService;
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;

    public AvailabilityModel(
        ITableAvailabilityService tableAvailabilityService,
        IRestaurantTableService tableService,
        IBookingService bookingService,
        IOrderService orderService,
        ICustomerService customerService)
    {
        _tableAvailabilityService = tableAvailabilityService;
        _tableService = tableService;
        _bookingService = bookingService;
        _orderService = orderService;
        _customerService = customerService;
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

        var previousStatus = booking.BookingStatus;

        if (previousStatus != status)
        {
            booking.BookingStatus = status;

            await _bookingService.UpdateAsync(booking);
            await _bookingService.SaveChangesAsync();

            await SyncRelatedOrdersAsync(bookingId, booking.CustomerId, status);
        }

        return RedirectToPage("/Booking/Availability", new
        {
            selectedDate = selectedDate.ToString("yyyy-MM-dd")
        });
    }

    private async Task SyncRelatedOrdersAsync(
        long bookingId,
        long? bookingCustomerId,
        BookingStatusEnum bookingStatus)
    {
        var newOrderStatus = MapBookingStatusToOrderStatus(bookingStatus);
        var orders = await _orderService.GetOrdersByBookingIdAsync(bookingId);

        foreach (var order in orders)
        {
            var previousOrderStatus = order.OrderStatus;

            order.OrderStatus = newOrderStatus;
            order.CompletedAt = newOrderStatus == OrderStatusEnum.COMPLETED
                ? DateTime.Now
                : null;

            await _orderService.UpdateOrderAsync(order);
            await SyncCustomerLoyaltyPointsAsync(
                order,
                bookingCustomerId,
                previousOrderStatus,
                newOrderStatus);
        }
    }

    private static OrderStatusEnum MapBookingStatusToOrderStatus(
        BookingStatusEnum bookingStatus)
    {
        return bookingStatus switch
        {
            BookingStatusEnum.PREPARING => OrderStatusEnum.PREPARING,
            BookingStatusEnum.COMPLETED => OrderStatusEnum.COMPLETED,
            BookingStatusEnum.CANCELLED => OrderStatusEnum.CANCELLED,
            _ => OrderStatusEnum.PENDING
        };
    }

    private async Task SyncCustomerLoyaltyPointsAsync(
        OrderEntity order,
        long? bookingCustomerId,
        OrderStatusEnum previousStatus,
        OrderStatusEnum newStatus)
    {
        var pointDelta = CalculatePointDelta(
            order.TotalAmount,
            previousStatus,
            newStatus);

        if (pointDelta == 0)
        {
            return;
        }

        var customerId = order.CustomerId ?? bookingCustomerId;
        if (!customerId.HasValue)
        {
            return;
        }

        await _customerService.AdjustLoyaltyPointsAsync(
            customerId.Value,
            pointDelta);
    }

    private static int CalculatePointDelta(
        decimal totalAmount,
        OrderStatusEnum previousStatus,
        OrderStatusEnum newStatus)
    {
        var points = CalculateEarnedPoints(totalAmount);

        if (previousStatus != OrderStatusEnum.COMPLETED &&
            newStatus == OrderStatusEnum.COMPLETED)
        {
            return points;
        }

        if (previousStatus == OrderStatusEnum.COMPLETED &&
            newStatus != OrderStatusEnum.COMPLETED)
        {
            return -points;
        }

        return 0;
    }

    private static int CalculateEarnedPoints(decimal totalAmount)
    {
        if (totalAmount <= 0)
        {
            return 0;
        }

        return (int)Math.Floor(totalAmount / AmountPerLoyaltyPoint);
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

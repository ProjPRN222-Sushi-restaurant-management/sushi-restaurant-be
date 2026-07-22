using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Services.Policies;

using OrderEntity = BusinessObjects.Models.Order;

namespace _290526_SushiRestaurantManagement_BE.Pages.Booking;

public class ReservationsModel : PageModel
{
    private readonly IRestaurantTableService _tableService;
    private readonly IBookingService _bookingService;
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;

    public ReservationsModel(
        IRestaurantTableService tableService,
        IBookingService bookingService,
        IOrderService orderService,
        ICustomerService customerService)
    {
        _tableService = tableService;
        _bookingService = bookingService;
        _orderService = orderService;
        _customerService = customerService;
    }

    public DateOnly SelectedDate { get; set; }

    public PaginatedList<BusinessObjects.Models.Booking> Bookings { get; set; }
        = new(new List<BusinessObjects.Models.Booking>(), 0, 1, 10);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public async Task OnGetAsync(DateOnly? selectedDate)
    {
        SelectedDate = selectedDate ?? DateOnly.FromDateTime(DateTime.Today);

        var allBookings = (await _bookingService.GetByDateAsync(SelectedDate)).ToList();
        await NormalizeActiveStatusesAsync(allBookings);

        Bookings = PaginatedList<BusinessObjects.Models.Booking>.Create(
            allBookings,
            PageNumber,
            PageSize);
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(
        long bookingId,
        BookingStatusEnum status,
        DateOnly selectedDate)
    {
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        var previousStatus = booking.BookingStatus;
        var targetStatus = NormalizeRequestedStatus(booking, status);

        if (previousStatus != targetStatus)
        {
            booking.BookingStatus = targetStatus;

            await _bookingService.UpdateAsync(booking);
            await _bookingService.SaveChangesAsync();
            await UpdateTableStatusForBookingAsync(booking, targetStatus);

            await SyncRelatedOrdersAsync(bookingId, booking.CustomerId, targetStatus);
        }

        return RedirectToPage("/Booking/Reservations", new
        {
            selectedDate = selectedDate.ToString("yyyy-MM-dd")
        });
    }

    private async Task SyncRelatedOrdersAsync(
        long bookingId,
        long? bookingCustomerId,
        BookingStatusEnum bookingStatus)
    {
        var newOrderStatus = BookingStatusPolicy.ToOrderStatus(bookingStatus);
        var orders = await _orderService.GetOrdersByBookingIdAsync(bookingId);

        foreach (var order in orders)
        {
            var previousOrderStatus = order.OrderStatus;

            order.OrderStatus = newOrderStatus;
            order.CompletedAt = newOrderStatus == OrderStatusEnum.COMPLETED
                ? DateTime.Now
                : null;

            if (newOrderStatus == OrderStatusEnum.COMPLETED)
            {
                ApplyInvoiceIssuer(order);
            }
            else
            {
                ClearInvoiceIssuer(order);
            }

            await _orderService.UpdateOrderAsync(order);
            await SyncCustomerLoyaltyPointsAsync(
                order,
                bookingCustomerId,
                previousOrderStatus,
                newOrderStatus);
        }
    }

    private async Task NormalizeActiveStatusesAsync(
        IReadOnlyList<BusinessObjects.Models.Booking> bookings)
    {
        var changed = false;

        foreach (var booking in bookings)
        {
            if (booking.BookingStatus != BookingStatusEnum.PENDING &&
                booking.BookingStatus != BookingStatusEnum.PREPARING)
            {
                continue;
            }

            var targetStatus = BookingStatusPolicy.GetActiveStatus(booking, DateTime.Now);

            if (booking.BookingStatus == targetStatus)
            {
                await SyncActiveOrderStatusesAsync(booking.BookingId, targetStatus);
                await UpdateTableStatusForBookingAsync(booking, targetStatus);
                continue;
            }

            booking.BookingStatus = targetStatus;
            await _bookingService.UpdateAsync(booking);
            await SyncActiveOrderStatusesAsync(booking.BookingId, targetStatus);
            await UpdateTableStatusForBookingAsync(booking, targetStatus);
            changed = true;
        }

        if (changed)
        {
            await _bookingService.SaveChangesAsync();
        }
    }

    private async Task UpdateTableStatusForBookingAsync(
        BusinessObjects.Models.Booking booking,
        BookingStatusEnum bookingStatus)
    {
        await _tableService.UpdateTableStatusAsync(
            booking.TableId,
            BookingStatusPolicy.ToTableStatus(bookingStatus));
    }

    private async Task SyncActiveOrderStatusesAsync(
        long bookingId,
        BookingStatusEnum bookingStatus)
    {
        var orderStatus = BookingStatusPolicy.ToOrderStatus(bookingStatus);
        var orders = await _orderService.GetOrdersByBookingIdAsync(bookingId);

        foreach (var order in orders)
        {
            if (order.OrderStatus != OrderStatusEnum.PENDING &&
                order.OrderStatus != OrderStatusEnum.PREPARING)
            {
                continue;
            }

            if (order.OrderStatus == orderStatus)
            {
                continue;
            }

            order.OrderStatus = orderStatus;
            await _orderService.UpdateOrderAsync(order);
        }
    }

    private static BookingStatusEnum NormalizeRequestedStatus(
        BusinessObjects.Models.Booking booking,
        BookingStatusEnum requestedStatus)
    {
        if (requestedStatus != BookingStatusEnum.PENDING &&
            requestedStatus != BookingStatusEnum.PREPARING)
        {
            return requestedStatus;
        }

        return BookingStatusPolicy.GetActiveStatus(booking, DateTime.Now);
    }

    private async Task SyncCustomerLoyaltyPointsAsync(
        OrderEntity order,
        long? bookingCustomerId,
        OrderStatusEnum previousStatus,
        OrderStatusEnum newStatus)
    {
        var pointDelta = CalculatePointDelta(order, previousStatus, newStatus);

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
        OrderEntity order,
        OrderStatusEnum previousStatus,
        OrderStatusEnum newStatus)
    {
        var points = order.EarnedLoyaltyPoints > 0
            ? order.EarnedLoyaltyPoints
            : LoyaltyPolicy.CalculateEarnedPoints(order.TotalAmount);

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

    private void ApplyInvoiceIssuer(OrderEntity order)
    {
        if (order.InvoiceIssuedAt.HasValue)
        {
            return;
        }

        order.InvoiceIssuedAt = DateTime.Now;
        order.InvoiceStaffName = HttpContext.Session.GetString("StaffName") ?? "Không xác định";

        if (long.TryParse(HttpContext.Session.GetString("StaffId"), out var staffId) &&
            staffId > 0)
        {
            order.InvoiceStaffId = staffId;
        }
        else
        {
            order.InvoiceStaffId = null;
        }
    }

    private static void ClearInvoiceIssuer(OrderEntity order)
    {
        order.InvoiceIssuedAt = null;
        order.InvoiceStaffId = null;
        order.InvoiceStaffName = null;
    }
}

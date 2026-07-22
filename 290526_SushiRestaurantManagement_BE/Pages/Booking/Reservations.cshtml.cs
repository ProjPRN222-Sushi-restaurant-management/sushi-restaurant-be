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

    public PaginatedList<OrderEntity> Orders { get; set; }
        = new(new List<OrderEntity>(), 0, 1, 10);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public async Task OnGetAsync(DateOnly? selectedDate)
    {
        SelectedDate = selectedDate ?? DateOnly.FromDateTime(DateTime.Today);

        var startDateTime = SelectedDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = SelectedDate.ToDateTime(TimeOnly.MaxValue);

        var orders = (await _orderService.GetAllOrdersAsync())
            .Where(order =>
                order.CreatedAt.HasValue &&
                order.CreatedAt.Value >= startDateTime &&
                order.CreatedAt.Value <= endDateTime)
            .OrderByDescending(order => order.CreatedAt ?? DateTime.MinValue)
            .ThenByDescending(order => order.OrderId)
            .ToList();

        Orders = PaginatedList<OrderEntity>.Create(
            orders,
            PageNumber,
            PageSize);
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(
        int orderId,
        OrderStatusEnum status,
        DateOnly selectedDate,
        int pageNumber = 1)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        var previousStatus = order.OrderStatus;
        var targetStatus = NormalizeRequestedOrderStatus(order, status);

        if (previousStatus != targetStatus)
        {
            order.OrderStatus = targetStatus;
            order.CompletedAt = targetStatus == OrderStatusEnum.COMPLETED
                ? DateTime.Now
                : null;

            if (targetStatus == OrderStatusEnum.COMPLETED)
            {
                ApplyInvoiceIssuer(order);
            }
            else
            {
                ClearInvoiceIssuer(order);
            }

            await _orderService.UpdateOrderAsync(order);
            await SyncRelatedBookingAndTableAsync(order, targetStatus);
            await SyncCustomerLoyaltyPointsAsync(order, previousStatus, targetStatus);
        }

        return RedirectToPage("/Booking/Reservations", new
        {
            selectedDate = selectedDate.ToString("yyyy-MM-dd"),
            PageNumber = pageNumber
        });
    }

    private async Task SyncRelatedBookingAndTableAsync(
        OrderEntity order,
        OrderStatusEnum orderStatus)
    {
        var bookingStatus = MapOrderStatusToBookingStatus(orderStatus);

        if (order.BookingId.HasValue)
        {
            await _bookingService.UpdateBookingStatusAsync(
                order.BookingId.Value,
                bookingStatus);
        }

        if (order.TableId.HasValue)
        {
            await _tableService.UpdateTableStatusAsync(
                order.TableId.Value,
                BookingStatusPolicy.ToTableStatus(bookingStatus));
        }
    }

    private static BookingStatusEnum MapOrderStatusToBookingStatus(
        OrderStatusEnum status)
    {
        return status switch
        {
            OrderStatusEnum.PREPARING => BookingStatusEnum.PREPARING,
            OrderStatusEnum.COMPLETED => BookingStatusEnum.COMPLETED,
            OrderStatusEnum.CANCELLED => BookingStatusEnum.CANCELLED,
            _ => BookingStatusEnum.PENDING
        };
    }

    private static OrderStatusEnum NormalizeRequestedOrderStatus(
        OrderEntity order,
        OrderStatusEnum requestedStatus)
    {
        if (requestedStatus != OrderStatusEnum.PENDING &&
            requestedStatus != OrderStatusEnum.PREPARING)
        {
            return requestedStatus;
        }

        if (order.Booking == null)
        {
            return requestedStatus;
        }

        var bookingStatus = BookingStatusPolicy.GetActiveStatus(
            order.Booking,
            DateTime.Now);

        return BookingStatusPolicy.ToOrderStatus(bookingStatus);
    }

    private async Task SyncCustomerLoyaltyPointsAsync(
        OrderEntity order,
        OrderStatusEnum previousStatus,
        OrderStatusEnum newStatus)
    {
        var pointDelta = CalculatePointDelta(order, previousStatus, newStatus);
        if (pointDelta == 0)
        {
            return;
        }

        var customerId = order.CustomerId ?? order.Booking?.CustomerId;
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

        if (order.ReceivedStaffId.HasValue ||
            !string.IsNullOrWhiteSpace(order.ReceivedStaffName))
        {
            order.InvoiceStaffId = order.ReceivedStaffId;
            order.InvoiceStaffName =
                order.ReceivedStaff?.FullName ??
                order.ReceivedStaffName ??
                "Không xác định";
            return;
        }

        order.InvoiceStaffName = HttpContext.Session.GetString("StaffName") ?? "Không xác định";

        if (long.TryParse(HttpContext.Session.GetString("StaffId"), out var staffId) &&
            staffId > 0)
        {
            order.InvoiceStaffId = staffId;
            return;
        }

        order.InvoiceStaffId = null;
    }

    private static void ClearInvoiceIssuer(OrderEntity order)
    {
        order.InvoiceIssuedAt = null;
        order.InvoiceStaffId = null;
        order.InvoiceStaffName = null;
    }
}

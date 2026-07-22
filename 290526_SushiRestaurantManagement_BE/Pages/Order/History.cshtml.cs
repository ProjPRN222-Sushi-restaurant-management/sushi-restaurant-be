using BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Services.Policies;

namespace _290526_SushiRestaurantManagement_BE.Pages.Order;

public class HistoryModel : PageModel
{
    private readonly IBookingService _bookingService;
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly IRestaurantTableService _tableService;

    public HistoryModel(
        IBookingService bookingService,
        IOrderService orderService,
        ICustomerService customerService,
        IRestaurantTableService tableService)
    {
        _bookingService = bookingService;
        _orderService = orderService;
        _customerService = customerService;
        _tableService = tableService;
    }

    public decimal TemporaryTotal { get; set; }

    public BusinessObjects.Models.Booking? Booking { get; set; }

    public IEnumerable<BusinessObjects.Models.Order> Orders { get; set; }
        = new List<BusinessObjects.Models.Order>();

    [BindProperty(SupportsGet = true)]
    public bool BillPrint { get; set; }

    public bool IsTakeawayOrder => Booking == null && Orders.Any();

    public async Task<IActionResult> OnGetAsync(long? bookingId, long? orderId)
    {
        if (orderId.HasValue)
        {
            var order = await _orderService.GetOrderByIdAsync((int)orderId.Value);
            if (order == null)
            {
                return NotFound();
            }

            Booking = order.Booking;
            Orders = new List<BusinessObjects.Models.Order> { order };
            TemporaryTotal = GetOrderPayableAmount(order);
            return Page();
        }

        if (!bookingId.HasValue)
        {
            return NotFound();
        }

        try
        {
            var result = await _bookingService.GetBookingOrderHistoryAsync(bookingId.Value);

            Booking = result.Booking;
            Orders = result.Orders;
            TemporaryTotal = result.TemporaryTotal;

            return Page();
        }
        catch
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostPayAndPrintBillAsync(long? bookingId, long? orderId)
    {
        if (orderId.HasValue)
        {
            return await PayAndPrintSingleOrderAsync(orderId.Value);
        }

        if (bookingId.HasValue)
        {
            return await PayAndPrintBookingOrdersAsync(bookingId.Value);
        }

        return NotFound();
    }

    private async Task<IActionResult> PayAndPrintSingleOrderAsync(long orderId)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync((int)orderId);
            if (order == null)
            {
                return NotFound();
            }

            var previousStatus = order.OrderStatus;
            order.OrderStatus = OrderStatusEnum.COMPLETED;
            order.CompletedAt ??= DateTime.Now;
            ApplyInvoiceIssuer(order);

            await _orderService.UpdateOrderAsync(order);
            await SyncCustomerLoyaltyPointsAsync(order, previousStatus, null);

            if (order.TableId.HasValue)
            {
                await _tableService.UpdateTableStatusAsync(
                    order.TableId.Value,
                    TableStatusEnum.AVAILABLE);
            }

            TempData["Success"] = "Thanh toán thành công. Hóa đơn đã sẵn sàng để in.";

            return RedirectToPage(new
            {
                orderId,
                billPrint = true
            });
        }
        catch
        {
            TempData["Error"] = "Không thể thanh toán đơn này.";
            return RedirectToPage(new { orderId });
        }
    }

    private async Task<IActionResult> PayAndPrintBookingOrdersAsync(long bookingId)
    {
        try
        {
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);
            var orders = (await _orderService.GetOrdersByBookingIdAsync(bookingId)).ToList();

            if (!orders.Any())
            {
                TempData["Error"] = "Đặt bàn này chưa có đơn để thanh toán.";
                return RedirectToPage(new { bookingId });
            }

            foreach (var order in orders)
            {
                var previousStatus = order.OrderStatus;

                order.OrderStatus = OrderStatusEnum.COMPLETED;
                order.CompletedAt ??= DateTime.Now;
                ApplyInvoiceIssuer(order);

                await _orderService.UpdateOrderAsync(order);
                await SyncCustomerLoyaltyPointsAsync(order, previousStatus, booking.CustomerId);
            }

            await _bookingService.UpdateBookingStatusAsync(
                bookingId,
                BookingStatusEnum.COMPLETED);

            await _tableService.UpdateTableStatusAsync(
                booking.TableId,
                TableStatusEnum.AVAILABLE);

            TempData["Success"] = "Thanh toán thành công. Hóa đơn đã sẵn sàng để in.";

            return RedirectToPage(new
            {
                bookingId,
                billPrint = true
            });
        }
        catch
        {
            TempData["Error"] = "Không thể thanh toán đặt bàn này.";
            return RedirectToPage(new { bookingId });
        }
    }

    private async Task SyncCustomerLoyaltyPointsAsync(
        BusinessObjects.Models.Order order,
        OrderStatusEnum previousStatus,
        long? bookingCustomerId)
    {
        if (previousStatus == OrderStatusEnum.COMPLETED)
        {
            return;
        }

        var customerId = order.CustomerId ?? bookingCustomerId;
        if (!customerId.HasValue)
        {
            return;
        }

        var points = order.EarnedLoyaltyPoints > 0
            ? order.EarnedLoyaltyPoints
            : LoyaltyPolicy.CalculateEarnedPoints(GetOrderPayableAmount(order));

        await _customerService.AdjustLoyaltyPointsAsync(
            customerId.Value,
            points);
    }

    private void ApplyInvoiceIssuer(BusinessObjects.Models.Order order)
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

    private static decimal GetOrderPayableAmount(BusinessObjects.Models.Order order)
    {
        if (order.TotalAmount > 0)
        {
            return order.TotalAmount;
        }

        if (order.SubtotalAmount > 0)
        {
            return order.SubtotalAmount - order.DiscountAmount;
        }

        return order.OrderItems.Sum(item => item.TotalPrice);
    }
}

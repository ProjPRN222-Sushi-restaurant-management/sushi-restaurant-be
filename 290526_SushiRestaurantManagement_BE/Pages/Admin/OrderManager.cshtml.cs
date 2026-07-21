using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Policies;

using OrderEntity = BusinessObjects.Models.Order;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class OrderManagerModel : AdminPageModel
    {
        private readonly IOrderService _orderService;
        private readonly IBookingService _bookingService;
        private readonly ICustomerService _customerService;

        public OrderManagerModel(
            IOrderService orderService,
            IBookingService bookingService,
            ICustomerService customerService)
        {
            _orderService = orderService;
            _bookingService = bookingService;
            _customerService = customerService;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public OrderStatusEnum? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public PaginatedList<OrderEntity> Orders{ get; set; }

        = new PaginatedList<OrderEntity>(
            new List<OrderEntity>(),
            0,
            1,
            10
        );

        public async Task OnGetAsync()
        {
            var allOrders =
                await _orderService.GetAllOrdersAsync();

            var query =
                allOrders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                var keyword = SearchString.Trim();

                query = query.Where(o =>

                    o.OrderId.ToString()
                     .Contains(keyword)

                    ||

                    (o.BookingId.HasValue &&
                     o.BookingId.Value.ToString()
                     .Contains(keyword)
                    )

                    ||

                    (o.TableId.HasValue &&
                     o.TableId.Value.ToString()
                     .Contains(keyword)
                    )

                    ||

                    (o.Customer != null &&
                     o.Customer.Phone != null &&

                     o.Customer.Phone
                      .Contains(keyword))

                    ||

                    (o.Customer != null &&
                     o.Customer.FullName != null &&

                     o.Customer.FullName
                      .Contains(keyword))

                    ||

                    (o.Booking != null &&
                     o.Booking.Customer != null &&
                     o.Booking.Customer.Phone != null &&

                     o.Booking.Customer.Phone
                      .Contains(keyword))

                    ||

                    (o.Booking != null &&
                     o.Booking.Customer != null &&
                     o.Booking.Customer.FullName != null &&

                     o.Booking.Customer.FullName
                      .Contains(keyword))
                );
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(o =>
                    o.OrderStatus ==
                    StatusFilter.Value);
            }

            var orderedOrders =
                query.OrderByDescending(
                    o => o.OrderId
                ).ToList();

            Orders =
                PaginatedList<OrderEntity>
                .Create(
                    orderedOrders,
                    PageNumber,
                    PageSize
                );
        }

        public async Task<IActionResult>
            OnPostUpdateStatusAsync(
            int orderId,
            OrderStatusEnum status)
        {
            var order =
                await _orderService
                .GetOrderByIdAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            var previousStatus = order.OrderStatus;
            status = NormalizeRequestedOrderStatus(order, status);

            order.OrderStatus = status;

            if (status ==
                OrderStatusEnum.COMPLETED)
            {
                order.CompletedAt =
                    DateTime.Now;

                ApplyInvoiceIssuer(order);
            }
            else
            {
                order.CompletedAt = null;
                ClearInvoiceIssuer(order);
            }


            await _orderService
                .UpdateOrderAsync(order);

            if (order.BookingId.HasValue)
            {
                await _bookingService.UpdateBookingStatusAsync(
                    order.BookingId.Value,
                    MapOrderStatusToBookingStatus(status));
            }

            await SyncCustomerLoyaltyPointsAsync(order, previousStatus, status);

            return RedirectToPage(
                "/Admin/OrderManager",
                new
                {
                    SearchString,
                    StatusFilter,
                    PageNumber
                }
            );
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
            var pointDelta = CalculatePointDelta(
                order,
                previousStatus,
                newStatus);

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
                    "Khong xac dinh";
                return;
            }

            order.InvoiceStaffName = HttpContext.Session.GetString("StaffName") ?? "Khong xac dinh";

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
}

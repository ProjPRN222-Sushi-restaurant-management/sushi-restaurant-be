using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

using OrderEntity = BusinessObjects.Models.Order;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class OrderManagerModel : AdminPageModel
    {
        private const decimal AmountPerLoyaltyPoint = 10000m;

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

                    o.BookingId.ToString()
                     .Contains(keyword)

                    ||

                    o.TableId.ToString()
                     .Contains(keyword)

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

            order.OrderStatus = status;

            if (status ==
                OrderStatusEnum.COMPLETED)
            {
                order.CompletedAt =
                    DateTime.Now;
            }
            else
            {
                order.CompletedAt = null;
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

        private async Task SyncCustomerLoyaltyPointsAsync(
            OrderEntity order,
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
    }
}

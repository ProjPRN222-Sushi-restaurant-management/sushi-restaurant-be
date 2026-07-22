using _290526_SushiRestaurantManagement_BE.Helpers;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using BookingEntity = BusinessObjects.Models.Booking;
using OrderEntity = BusinessObjects.Models.Order;

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class IndexModel : AdminPageModel
    {
        private readonly IBookingService _bookingService;
        private readonly IOrderService _orderService;
        private readonly ITableAvailabilityService _tableAvailabilityService;

        public IndexModel(
            IBookingService bookingService,
            IOrderService orderService,
            ITableAvailabilityService tableAvailabilityService)
        {
            _bookingService = bookingService;
            _orderService = orderService;
            _tableAvailabilityService = tableAvailabilityService;
        }

        [BindProperty(SupportsGet = true)]
        public DateOnly? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? EndDate { get; set; }

        public int TotalBookingsCount { get; set; }
        public int TotalOrdersCount { get; set; }
        public int AvailableTablesCount { get; set; }
        public decimal MonthlyRevenue { get; set; }

        public PaginatedList<BookingEntity> FilteredBookings { get; set; } = new(new List<BookingEntity>(), 0, 1, 10);
        public PaginatedList<OrderEntity> FilteredOrders { get; set; } = new(new List<OrderEntity>(), 0, 1, 10);

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var nowTime = TimeOnly.FromDateTime(DateTime.Now);

            StartDate ??= new DateOnly(today.Year, today.Month, 1);
            EndDate ??= today;

            var allBookings = await _bookingService.GetAllBookingsAsync();
            var bookingsInRange = allBookings
                .Where(b => b.BookingDate >= StartDate.Value && b.BookingDate <= EndDate.Value)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.BookingTime)
                .ToList();

            FilteredBookings = PaginatedList<BookingEntity>.Create(bookingsInRange, PageNumber, PageSize);
            TotalBookingsCount = bookingsInRange.Count;

            var allOrders = await _orderService.GetAllOrdersAsync();
            var startDateTime = StartDate.Value.ToDateTime(TimeOnly.MinValue);
            var endDateTime = EndDate.Value.ToDateTime(TimeOnly.MaxValue);

            var ordersInRange = allOrders
                .Where(o =>
                    o.CreatedAt.HasValue &&
                    o.CreatedAt.Value >= startDateTime &&
                    o.CreatedAt.Value <= endDateTime)
                .OrderByDescending(o => o.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(o => o.OrderId)
                .ToList();

            FilteredOrders = PaginatedList<OrderEntity>.Create(ordersInRange, PageNumber, PageSize);
            TotalOrdersCount = ordersInRange.Count;
            MonthlyRevenue = ordersInRange.Sum(GetOrderPayableAmount);

            try
            {
                AvailableTablesCount = await _tableAvailabilityService.GetAvailableTableCountAsync(today, nowTime);
            }
            catch
            {
                AvailableTablesCount = 0;
            }
        }

        private static decimal GetOrderPayableAmount(OrderEntity order)
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
}

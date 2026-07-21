using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Enums;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Cart
{
    public class CheckoutModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IBookingService _bookingService;

        public CheckoutModel(
            IOrderService orderService,
            IBookingService bookingService)
        {
            _orderService = orderService;
            _bookingService = bookingService;
        }

        public List<CartItemViewModel> CartItems { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public int? OrderId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool Print { get; set; }

        public BusinessObjects.Models.Order? SavedOrder { get; set; }

        public decimal TotalAmount => CartItems.Sum(x => x.Total);

        public async Task<IActionResult> OnGetAsync()
        {
            if (OrderId.HasValue)
            {
                try
                {
                    SavedOrder = await _orderService.GetOrderByIdAsync(OrderId.Value);
                    CartItems = SavedOrder.OrderItems.Select(x => new CartItemViewModel
                    {
                        MenuItemId = x.MenuItemId,
                        ItemName = x.MenuItem?.ItemName ?? $"Món #{x.MenuItemId}",
                        UnitPrice = x.UnitPrice,
                        Quantity = x.Quantity,
                        Note = x.Note
                    }).ToList();
                }
                catch (KeyNotFoundException)
                {
                    TempData["Error"] = "Không tìm thấy order vừa gửi.";
                    return RedirectToPage("/Cart/Index");
                }

                return Page();
            }

            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];

            if (!CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng đang trống.";
                return RedirectToPage("/Cart/Index");
            }

            long? bookingId = null;
            long? tableId = null;

            if (long.TryParse(HttpContext.Session.GetString("BOOKING_ID"), out var bId))
                bookingId = bId;

            if (long.TryParse(HttpContext.Session.GetString("TABLE_ID"), out var tId))
                tableId = tId;

            BusinessObjects.Models.Booking? booking = null;
            if (bookingId.HasValue)
            {
                booking = await _bookingService.GetBookingByIdAsync(bookingId.Value);
                tableId ??= booking?.TableId;
            }

            var now = DateTime.Now;
            var order = new BusinessObjects.Models.Order
            {
                BookingId = bookingId,
                CustomerId = booking?.CustomerId,
                TableId = tableId,
                TotalAmount = TotalAmount,
                OrderStatus = OrderStatusEnum.PREPARING,
                CreatedAt = now,
                ReceivedAt = now,
                ReceivedStaffId = TryGetCurrentStaffId(),
                ReceivedStaffName = HttpContext.Session.GetString("StaffName") ?? "Không xác định",
                OrderItems = CartItems.Select(x => new OrderItem
                {
                    MenuItemId = x.MenuItemId,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    TotalPrice = x.Total,
                    Note = x.Note
                }).ToList()
            };

            await _orderService.AddOrderAsync(order);
            await _orderService.SaveChangesAsync();

            if (bookingId.HasValue)
            {
                await _bookingService.UpdateBookingStatusAsync(
                    bookingId.Value,
                    BookingStatusEnum.PREPARING);
            }

            HttpContext.Session.Remove("CART");

            TempData["OrderSuccess"] = $"Gửi order thành công! Mã order #{order.OrderId}";

            return RedirectToPage("/Cart/Checkout", new
            {
                orderId = order.OrderId,
                print = true
            });
        }

        private long? TryGetCurrentStaffId()
        {
            if (long.TryParse(HttpContext.Session.GetString("StaffId"), out var staffId) &&
                staffId > 0)
            {
                return staffId;
            }

            return null;
        }
    }
}
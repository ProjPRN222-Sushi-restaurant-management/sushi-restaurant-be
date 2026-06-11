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

        public CheckoutModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public List<CartItemViewModel> CartItems { get; set; } = [];

        public decimal TotalAmount => CartItems.Sum(x => x.Total);

        public void OnGet()
        {
            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];
        }

        public async Task<IActionResult> OnPostAsync()
        {
            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];

            if (!CartItems.Any())
            {
                TempData["Error"] = "Gi? hàng ?ang tr?ng.";
                return RedirectToPage("/Cart/Index");
            }

            long? bookingId = null;
            long? tableId = null;

            if (long.TryParse(HttpContext.Session.GetString("BOOKING_ID"), out var bId))
                bookingId = bId;

            if (long.TryParse(HttpContext.Session.GetString("TABLE_ID"), out var tId))
                tableId = tId;

            var order = new BusinessObjects.Models.Order
            {
                BookingId = bookingId,
                TableId = tableId,
                TotalAmount = TotalAmount,
                OrderStatus = OrderStatusEnum.PENDING,
                CreatedAt = DateTime.Now,
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

            HttpContext.Session.Remove("CART");

            TempData["OrderSuccess"] = $"G?i order thành công! Mã order #{order.OrderId}";

            return RedirectToPage("/Cart/Checkout");
        }
    }
}

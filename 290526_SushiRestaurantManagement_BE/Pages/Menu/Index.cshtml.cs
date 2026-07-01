using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Menu
{
    public class IndexModel : PageModel
    {
        private const string CartSessionKey = "CART";

        private readonly IMenuItemService _menuItemService;

        public IndexModel(IMenuItemService menuItemService)
        {
            _menuItemService = menuItemService;
        }

        public IReadOnlyList<MenuItem> MenuItems { get; set; } = [];

        public List<CartItemViewModel> CartItems { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public long? BookingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? TableId { get; set; }

        public async Task OnGetAsync()
        {
            MenuItems = await _menuItemService.GetAllMenuItemsAsync();
            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>(CartSessionKey) ?? [];
        }

        public async Task<IActionResult> OnPostAddToCartAsync(
            long menuItemId,
            int quantity,
            string? note,
            long? bookingId,
            long? tableId)
        {
            if (quantity < 1)
                quantity = 1;

            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>(CartSessionKey) ?? [];

            var menuItem = await _menuItemService.GetMenuItemByIdAsync((int)menuItemId);

            if (menuItem != null)
            {
                var existing = cart.FirstOrDefault(x =>
                    x.MenuItemId == menuItemId &&
                    x.Note == note);

                if (existing == null)
                {
                    cart.Add(new CartItemViewModel
                    {
                        MenuItemId = menuItem.MenuItemId,
                        ItemName = menuItem.ItemName,
                        UnitPrice = menuItem.Price,
                        Quantity = quantity,
                        Note = note
                    });
                }
                else
                {
                    existing.Quantity += quantity;
                }

                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            if (bookingId != null)
                HttpContext.Session.SetString("BOOKING_ID", bookingId.Value.ToString());

            if (tableId != null)
                HttpContext.Session.SetString("TABLE_ID", tableId.Value.ToString());

            var isAjax = Request.Headers["X-Requested-With"]
                .ToString()
                .Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            if (isAjax)
            {
                return new JsonResult(new
                {
                    success = true,
                    count = cart.Sum(x => x.Quantity),
                    totalAmount = cart.Sum(x => x.Total),
                    items = cart.Select(x => new
                    {
                        menuItemId = x.MenuItemId,
                        itemName = x.ItemName,
                        quantity = x.Quantity,
                        unitPrice = x.UnitPrice,
                        total = x.Total,
                        note = x.Note
                    })
                });
            }

            // Fallback cho trường hợp không có JavaScript
            TempData["Success"] = "Đã thêm món vào giỏ hàng.";
            return RedirectToPage("/Menu/Index", new { bookingId, tableId });
        }
    }
}

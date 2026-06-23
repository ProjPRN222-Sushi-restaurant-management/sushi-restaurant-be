using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly IMenuItemService _menuItemService;

        public IndexModel(IMenuItemService menuItemService)
        {
            _menuItemService = menuItemService;
        }

        [BindProperty(SupportsGet = true)]
        public long? BookingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? TableId { get; set; }

        public List<CartItemViewModel> CartItems { get; set; } = [];

        public decimal TotalAmount => CartItems.Sum(x => x.Total);

        public void OnGet()
        {
            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];
        }

        public async Task<IActionResult> OnPostAddAsync(
            long menuItemId,
            int quantity,
            string? note,
            long? bookingId,
            long? tableId,
            string? returnUrl,
            int? scrollY)
        {
            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];

            var menuItem = await _menuItemService.GetMenuItemByIdAsync((int)menuItemId);

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

            HttpContext.Session.SetObject("CART", cart);

            if (bookingId != null)
                HttpContext.Session.SetString("BOOKING_ID", bookingId.Value.ToString());

            if (tableId != null)
                HttpContext.Session.SetString("TABLE_ID", tableId.Value.ToString());

            TempData["Success"] = "Đã thêm món vào giỏ hàng.";

            return RedirectToPage("/Menu/Index", new
            {
                bookingId = bookingId,
                tableId = tableId,
                scrollY = scrollY
            });
        }

        public IActionResult OnPostUpdateQuantity(
            long menuItemId,
            string? note,
            int quantity)
        {
            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];

            var item = cart.FirstOrDefault(x =>
                x.MenuItemId == menuItemId &&
                x.Note == note);

            if (item == null)
            {
                return RedirectToPage();
            }

            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            HttpContext.Session.SetObject("CART", cart);

            return RedirectToPage();
        }

        //public IActionResult OnPostRemove(long menuItemId)
        //{
        //    var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];

        //    cart.RemoveAll(x => x.MenuItemId == menuItemId);

        //    HttpContext.Session.SetObject("CART", cart);

        //    return RedirectToPage();
        //}
        public IActionResult OnPostRemove(long menuItemId, string? note)
        {
            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];

            var item = cart.FirstOrDefault(x =>
                x.MenuItemId == menuItemId &&
                x.Note == note);

            if (item != null)
            {
                cart.Remove(item);
            }

            HttpContext.Session.SetObject("CART", cart);

            return RedirectToPage();
        }

        public IActionResult OnPostIncrease(long menuItemId, string? note)
        {
            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];

            var item = cart.FirstOrDefault(x =>
                x.MenuItemId == menuItemId &&
                x.Note == note);

            if (item != null)
            {
                item.Quantity += 1;
            }

            HttpContext.Session.SetObject("CART", cart);

            return RedirectToPage();
        }

        public IActionResult OnPostDecrease(long menuItemId, string? note)
        {
            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];

            var item = cart.FirstOrDefault(x =>
                x.MenuItemId == menuItemId &&
                x.Note == note);

            if (item != null)
            {
                item.Quantity -= 1;

                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }
            }

            HttpContext.Session.SetObject("CART", cart);

            return RedirectToPage();
        }
    }
}

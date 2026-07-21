using _290526_SushiRestaurantManagement_BE.Helpers;
using BusinessObjects.Enums;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Services.Policies;

namespace _290526_SushiRestaurantManagement_BE.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly IMenuItemService _menuItemService;
        private readonly IBookingService _bookingService;

        public IndexModel(
            IMenuItemService menuItemService,
            IBookingService bookingService)
        {
            _menuItemService = menuItemService;
            _bookingService = bookingService;
        }

        [BindProperty(SupportsGet = true)]
        public long? BookingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? TableId { get; set; }

        public List<CartItemViewModel> CartItems { get; set; } = [];

        public MembershipLevelEnum CustomerMembershipLevel { get; set; }

        public int CustomerLoyaltyPoints { get; set; }

        public decimal SubtotalAmount => CartItems.Sum(x => x.Total);

        public decimal DiscountPercent =>
            LoyaltyPolicy.GetDiscountPercent(CustomerMembershipLevel);

        public decimal DiscountAmount =>
            LoyaltyPolicy.CalculateDiscountAmount(SubtotalAmount, CustomerMembershipLevel);

        public decimal TotalAmount =>
            LoyaltyPolicy.CalculatePayableAmount(SubtotalAmount, CustomerMembershipLevel);

        public int EarnedPoints =>
            LoyaltyPolicy.CalculateEarnedPoints(TotalAmount);

        public async Task OnGetAsync()
        {
            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>("CART") ?? [];
            await LoadCustomerLoyaltyContextAsync();
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
            {
                HttpContext.Session.SetString("BOOKING_ID", bookingId.Value.ToString());
            }

            if (tableId != null)
            {
                HttpContext.Session.SetString("TABLE_ID", tableId.Value.ToString());
            }

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

        private async Task LoadCustomerLoyaltyContextAsync()
        {
            if (!long.TryParse(HttpContext.Session.GetString("BOOKING_ID"), out var bookingId))
            {
                return;
            }

            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                CustomerMembershipLevel = booking.Customer?.MembershipLevel ?? MembershipLevelEnum.NONE;
                CustomerLoyaltyPoints = booking.Customer?.LoyaltyPoints ?? 0;
            }
            catch (KeyNotFoundException)
            {
                CustomerMembershipLevel = MembershipLevelEnum.NONE;
                CustomerLoyaltyPoints = 0;
            }
        }
    }
}
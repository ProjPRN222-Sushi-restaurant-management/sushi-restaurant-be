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
        private const int PageSize = 8;

        private readonly IMenuItemService _menuItemService;
        private readonly IMenuCategoryService _menuCategoryService;

        public IndexModel(
            IMenuItemService menuItemService,
            IMenuCategoryService menuCategoryService)
        {
            _menuItemService = menuItemService;
            _menuCategoryService = menuCategoryService;
        }

        public IReadOnlyList<MenuItem> MenuItems { get; set; } = [];

        public IReadOnlyList<MenuCategory> Categories { get; set; } = [];

        public List<CartItemViewModel> CartItems { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public long? BookingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? TableId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalItems { get; set; }

        public int TotalPages { get; set; } = 1;

        public int FirstItemNumber =>
            TotalItems == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

        public int LastItemNumber =>
            Math.Min(PageNumber * PageSize, TotalItems);

        public async Task OnGetAsync()
        {
            var items = await _menuItemService.GetAllMenuItemsAsync();
            Categories = await _menuCategoryService.GetAllMenuCategoriesAsync();

            var query = items.AsEnumerable();

            if (CategoryId.HasValue)
            {
                query = query.Where(item => item.CategoryId == CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var keyword = SearchTerm.Trim();
                query = query.Where(item =>
                    item.ItemName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (item.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            TotalItems = query.Count();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
            PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

            MenuItems = query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
            CartItems = HttpContext.Session.GetObject<List<CartItemViewModel>>(CartSessionKey) ?? [];
        }

        public async Task<IActionResult> OnPostAddToCartAsync(
            long menuItemId,
            int quantity,
            string? note,
            string? wasabiOption,
            string? gingerOption,
            long? bookingId,
            long? tableId,
            string? searchTerm,
            long? categoryId,
            int pageNumber = 1)
        {
            if (quantity < 1)
                quantity = 1;

            var structuredNote = BuildStructuredNote(note, wasabiOption, gingerOption);
            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>(CartSessionKey) ?? [];

            var menuItem = await _menuItemService.GetMenuItemByIdAsync((int)menuItemId);

            if (menuItem != null)
            {
                var existing = cart.FirstOrDefault(x =>
                    x.MenuItemId == menuItemId &&
                    x.Note == structuredNote);

                if (existing == null)
                {
                    cart.Add(new CartItemViewModel
                    {
                        MenuItemId = menuItem.MenuItemId,
                        ItemName = menuItem.ItemName,
                        UnitPrice = menuItem.Price,
                        Quantity = quantity,
                        Note = structuredNote
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
                return new JsonResult(BuildCartPayload(cart));
            }

            TempData["Success"] = "Đã thêm món vào giỏ hàng.";
            return RedirectToPage("/Menu/Index", new
            {
                bookingId,
                tableId,
                searchTerm,
                categoryId,
                pageNumber
            });
        }

        public IActionResult OnPostRemoveFromCart(long menuItemId, string? note)
        {
            var cart = HttpContext.Session.GetObject<List<CartItemViewModel>>(CartSessionKey) ?? [];

            var item = cart.FirstOrDefault(x =>
                x.MenuItemId == menuItemId &&
                x.Note == note);

            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetObject(CartSessionKey, cart);
            }

            return new JsonResult(BuildCartPayload(cart));
        }

        private static object BuildCartPayload(List<CartItemViewModel> cart) => new
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
        };

        private static string? BuildStructuredNote(string? note, string? wasabiOption, string? gingerOption)
        {
            var options = new List<string>();

            var wasabi = NormalizeYesNo(wasabiOption);
            if (wasabi != null)
                options.Add($"Wasabi: {wasabi}");

            var ginger = NormalizeYesNo(gingerOption);
            if (ginger != null)
                options.Add($"Gừng hồng: {ginger}");

            if (options.Count > 0)
                return string.Join("; ", options);

            note = note?.Trim();
            return string.IsNullOrWhiteSpace(note) ? null : note;
        }

        private static string? NormalizeYesNo(string? value)
        {
            value = value?.Trim();

            return value switch
            {
                "Có" or "Co" or "Yes" or "true" or "True" => "Có",
                "Không" or "Khong" or "No" or "false" or "False" => "Không",
                _ => null
            };
        }
    }
}


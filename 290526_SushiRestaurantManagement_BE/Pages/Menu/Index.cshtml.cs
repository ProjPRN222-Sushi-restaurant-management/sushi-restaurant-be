using BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace _290526_SushiRestaurantManagement_BE.Pages.Menu
{
    public class IndexModel : PageModel
    {
        private readonly IMenuItemService _menuItemService;

        public IndexModel(IMenuItemService menuItemService)
        {
            _menuItemService = menuItemService;
        }

        public IReadOnlyList<MenuItem> MenuItems { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public long? BookingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? TableId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ScrollY { get; set; }

        public async Task OnGetAsync()
        {
            BookingId = BookingId;
            TableId = TableId;
            MenuItems = await _menuItemService.GetAllMenuItemsAsync();
        }
    }
}

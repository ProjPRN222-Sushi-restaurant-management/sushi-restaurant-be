using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BusinessObjects.Models;
using DataAccessObjects; 

namespace _290526_SushiRestaurantManagement_BE.Pages.Admin
{
    public class EditMenuItemModel : PageModel
    {
        private readonly RestaurantSystemDbContext _context;

        public EditMenuItemModel(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MenuItem MenuItem { get; set; } = new MenuItem();

        public List<MenuCategory> Categories { get; set; } = new List<MenuCategory>();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Categories = await _context.MenuCategories.ToListAsync();

            var menuitem = await _context.MenuItems.FirstOrDefaultAsync(m => m.MenuItemId == id);
            if (menuitem == null)
            {
                return NotFound();
            }

            MenuItem = menuitem;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existingItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.MenuItemId == MenuItem.MenuItemId);

            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.ItemName = MenuItem.ItemName.Trim();
            existingItem.CategoryId = MenuItem.CategoryId;
            existingItem.Price = MenuItem.Price;
            existingItem.IsAvailable = MenuItem.IsAvailable == true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "C?p nh?t món ?n thành công!";

            return RedirectToPage("/Admin/MenuManager");
        }
    }
}
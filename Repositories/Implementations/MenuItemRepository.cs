using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class MenuItemRepository : IMenuItemRepository
    {
        private readonly RestaurantSystemDbContext _context;

        public MenuItemRepository(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<MenuItem>> GetAllMenuItemsAsync(CancellationToken ct = default)
        {
            return await _context.MenuItems
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.MenuItemId)
                .ToListAsync(ct);
        }

        public async Task<MenuItem> GetMenuItemByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var menuItem = await _context.MenuItems
                .AsNoTracking()
                .Include(mi => mi.Category)
                .FirstOrDefaultAsync(mi => mi.MenuItemId == id, ct);

            return menuItem ?? throw new KeyNotFoundException($"Menu item {id} not found.");
        }

        public async Task<bool> DeleteMenuItemAsync(
            int MenuItemId,
            CancellationToken ct = default)
        {
            var menuItem = await _context.MenuItems.FindAsync(new object[] { MenuItemId }, ct);
            if (menuItem == null)
            {
                return false; // Not found
            }
            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> AddMenuItemAsync(
            MenuItem item,
            CancellationToken ct = default)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemName))
            {
                return false;
            }
            item.ItemName = item.ItemName.Trim();
            try
            {
                _context.MenuItems.Add(item);
                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception)
            {
                // Log the exception as needed
                return false;
            }
        }

        public async Task<bool> UpdateMenuItemAsync(MenuItem menuItem, CancellationToken ct)
        {
            if (menuItem == null || string.IsNullOrWhiteSpace(menuItem.ItemName))
            {
                return false;
            }
            menuItem.ItemName = menuItem.ItemName.Trim();
            try
            {
                _context.MenuItems.Update(menuItem);
                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception)
            {
                // Log the exception as needed
                return false;
            }
        }
    }
}

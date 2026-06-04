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

        public async Task<IReadOnlyList<MenuItem>> GetAllMenuItemsAsync(
            CancellationToken ct = default)
        {
            return await _context.MenuItems
                .AsNoTracking()
                .Include(mi => mi.Category)
                .OrderBy(mi => mi.MenuItemId)
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
    }
}

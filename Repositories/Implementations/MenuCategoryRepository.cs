
using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class MenuCategoryRepository : IMenuCategoryRepository
    {
        private readonly RestaurantSystemDbContext _context;

        public MenuCategoryRepository(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<MenuCategory>> GetAllMenuCategoriesAsync(
            CancellationToken ct = default)
        {
            return await _context.MenuCategories
                .AsNoTracking()
                .OrderBy(c => c.CategoryId)
                .ToListAsync(ct);
        }

        public async Task<MenuCategory?> GetMenuCategoryByIdAsync(
            long id,
            CancellationToken ct = default)
        {
            return await _context.MenuCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id, ct);
        }
    }
}

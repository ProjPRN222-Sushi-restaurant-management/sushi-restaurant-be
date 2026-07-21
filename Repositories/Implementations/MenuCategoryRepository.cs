
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
                .Where(c => c.DeletedAt == null)
                .OrderBy(c => c.CategoryId)
                .ToListAsync(ct);
        }

        public async Task<MenuCategory?> GetMenuCategoryByIdAsync(
            long id,
            CancellationToken ct = default)
        {
            return await _context.MenuCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.DeletedAt == null, ct);
        }

        public async Task<bool> AddMenuCategoryAsync(
            MenuCategory category,
            CancellationToken ct = default)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.CategoryName))
            {
                return false;
            }
            category.CategoryName = category.CategoryName.Trim();
            try
            {
                _context.MenuCategories.Add(category);
                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception)
            {
                // Log the exception as needed
                return false;
            }
        }

        public async Task<bool> DeleteMenuCategoryAsync(long categoryId, CancellationToken ct = default)
        {
            var category = await _context.MenuCategories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category == null)
                return false;

            category.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

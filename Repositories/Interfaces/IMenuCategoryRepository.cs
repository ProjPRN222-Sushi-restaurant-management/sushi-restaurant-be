
using BusinessObjects.Models;

namespace Repositories.Interfaces
{
    public interface IMenuCategoryRepository
    {
        Task<IReadOnlyList<MenuCategory>> GetAllMenuCategoriesAsync(CancellationToken ct = default);
        Task<MenuCategory?> GetMenuCategoryByIdAsync(long id, CancellationToken ct = default);
        Task<bool> AddMenuCategoryAsync(MenuCategory category, CancellationToken ct = default);
    }
}

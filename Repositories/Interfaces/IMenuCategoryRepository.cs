
using BusinessObjects.Models;

namespace Repositories.Interfaces
{
    public interface IMenuCategoryRepository
    {
        Task<IReadOnlyList<MenuCategory>> GetAllMenuCategoriesAsync(CancellationToken ct = default);
        Task<MenuCategory?> GetMenuCategoryByIdAsync(int id, CancellationToken ct = default);
    }
}

using BusinessObjects.Models;

namespace Services.Interfaces;

public interface IMenuCategoryService
{
    Task<IReadOnlyList<MenuCategory>> GetAllMenuCategoriesAsync(CancellationToken ct = default);
    Task<MenuCategory?> GetMenuCategoryByIdAsync(long id, CancellationToken ct = default);
    Task<bool> AddMenuCategoryAsync(MenuCategory category, CancellationToken ct = default);
    Task<bool> DeleteMenuCategoryAsync(long id, CancellationToken ct = default);
}

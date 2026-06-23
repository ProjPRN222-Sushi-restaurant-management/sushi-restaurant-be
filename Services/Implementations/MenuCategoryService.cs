using BusinessObjects.Models;
using Repositories.Implementations;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class MenuCategoryService : IMenuCategoryService
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IMenuItemRepository _menuItemRepository;

    public MenuCategoryService(IMenuCategoryRepository menuCategoryRepository, IMenuItemRepository menuItemRepository)
    {
        _menuCategoryRepository = menuCategoryRepository;
        _menuItemRepository = menuItemRepository;
    }

    public Task<IReadOnlyList<MenuCategory>> GetAllMenuCategoriesAsync(CancellationToken ct = default)
        => _menuCategoryRepository.GetAllMenuCategoriesAsync(ct);

    public Task<MenuCategory?> GetMenuCategoryByIdAsync(long id, CancellationToken ct = default)
        => _menuCategoryRepository.GetMenuCategoryByIdAsync(id, ct);

    public async Task<bool> AddMenuCategoryAsync(MenuCategory category, CancellationToken ct = default)
    {
        // 1. Ki?m tra ràng bu?c d? li?u ??u vào (Validation)
        if (category == null || string.IsNullOrWhiteSpace(category.CategoryName))
        {
            return false;
        }

        // 2. Chu?n hóa d? li?u chu?i chu ?áo
        category.CategoryName = category.CategoryName.Trim();

        try
        {
            // 3. G?i xu?ng t?ng Repository x? lý l?u d? li?u (C?n ??m b?o Repo c?a b?n ?ã có hàm Add/Save ho?c t??ng ???ng)
            // N?u Repository s? d?ng mô hình chung (Generic) ho?c Entity Framework:
            await _menuCategoryRepository.AddMenuCategoryAsync(category, ct);
            return true;
        }
        catch (Exception)
        {
            // Ghi nh?n log l?i h? th?ng n?u c?n thi?t ? ?ây
            return false;
        }
    }

    public async Task<bool> DeleteMenuCategoryAsync(long categoryId, CancellationToken ct = default)
    {
        return await _menuCategoryRepository.DeleteMenuCategoryAsync(categoryId, ct);
    }
}

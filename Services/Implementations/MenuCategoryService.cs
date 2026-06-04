using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class MenuCategoryService : IMenuCategoryService
{
    private readonly IMenuCategoryRepository _menuCategoryRepository;

    public MenuCategoryService(IMenuCategoryRepository menuCategoryRepository)
    {
        _menuCategoryRepository = menuCategoryRepository;
    }

    public Task<IReadOnlyList<MenuCategory>> GetAllMenuCategoriesAsync(CancellationToken ct = default)
        => _menuCategoryRepository.GetAllMenuCategoriesAsync(ct);

    public Task<MenuCategory?> GetMenuCategoryByIdAsync(long id, CancellationToken ct = default)
        => _menuCategoryRepository.GetMenuCategoryByIdAsync(id, ct);
}

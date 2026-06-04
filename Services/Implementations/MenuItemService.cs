using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class MenuItemService : IMenuItemService
{
    private readonly IMenuItemRepository _menuItemRepository;

    public MenuItemService(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public Task<IReadOnlyList<MenuItem>> GetAllMenuItemsAsync(CancellationToken ct = default)
        => _menuItemRepository.GetAllMenuItemsAsync(ct);

    public Task<MenuItem> GetMenuItemByIdAsync(int id, CancellationToken ct = default)
        => _menuItemRepository.GetMenuItemByIdAsync(id, ct);
}

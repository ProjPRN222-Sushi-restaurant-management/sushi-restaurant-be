using BusinessObjects.Models;

namespace Services.Interfaces;

public interface IMenuItemService
{
    Task<IReadOnlyList<MenuItem>> GetAllMenuItemsAsync(CancellationToken ct = default);
    Task<MenuItem> GetMenuItemByIdAsync(int id, CancellationToken ct = default);
    Task<bool> AddMenuItemAsync(MenuItem item, CancellationToken ct = default);
    Task<bool> DeleteMenuItemAsync(long MenuItemId, CancellationToken ct = default);
    Task<bool> HasMenuItemsByCategoryAsync(long categoryId);
}

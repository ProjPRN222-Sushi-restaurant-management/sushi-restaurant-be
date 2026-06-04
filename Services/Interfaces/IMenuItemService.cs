using BusinessObjects.Models;

namespace Services.Interfaces;

public interface IMenuItemService
{
    Task<IReadOnlyList<MenuItem>> GetAllMenuItemsAsync(CancellationToken ct = default);
    Task<MenuItem> GetMenuItemByIdAsync(int id, CancellationToken ct = default);
}

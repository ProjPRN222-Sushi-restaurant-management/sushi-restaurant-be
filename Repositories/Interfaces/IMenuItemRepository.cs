
using BusinessObjects.Models;

namespace Repositories.Interfaces
{
    public interface IMenuItemRepository
    {
        Task<IReadOnlyList<MenuItem>> GetAllMenuItemsAsync(CancellationToken ct = default);
        Task<MenuItem> GetMenuItemByIdAsync(long id, CancellationToken ct = default);
        Task<bool> DeleteMenuItemAsync(long dishId, CancellationToken ct = default);
        Task<bool> AddMenuItemAsync(MenuItem item, CancellationToken ct = default);
        Task<bool> UpdateMenuItemAsync(MenuItem menuItem, CancellationToken ct);
    }
}


using BusinessObjects.Models;

namespace Repositories.Interfaces
{
    public interface IMenuItemRepository
    {
        Task<IReadOnlyList<MenuItem>> GetAllMenuItemsAsync(CancellationToken ct = default);
        Task<MenuItem> GetMenuItemByIdAsync(int id, CancellationToken ct = default);
    }
}

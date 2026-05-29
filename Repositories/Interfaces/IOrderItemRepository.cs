
using BusinessObjects.Models;

namespace Repositories.Interfaces
{
    public interface IOrderItemRepository
    {
        Task<IReadOnlyList<OrderItem>> GetAllOrderItemsAsync(CancellationToken ct = default);
        Task<OrderItem> GetOrderItemByIdAsync(int id, CancellationToken ct = default);
    }
}

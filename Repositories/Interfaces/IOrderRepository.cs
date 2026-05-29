

using BusinessObjects.Models;

namespace Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<IReadOnlyList<Order>> GetAllOrdersAsync(CancellationToken ct = default);
        Task<Order> GetOrderById(int id, CancellationToken ct = default);
    }
}

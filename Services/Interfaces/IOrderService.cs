using BusinessObjects.Models;

namespace Services.Interfaces;

public interface IOrderService
{
    Task<IReadOnlyList<Order>> GetAllOrdersAsync(CancellationToken ct = default);
    Task<Order> GetOrderByIdAsync(int id, CancellationToken ct = default);
}

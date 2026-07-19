using BusinessObjects.Models;

namespace Services.Interfaces;

public interface IOrderItemService
{
    Task<IReadOnlyList<OrderItem>> GetAllOrderItemsAsync(CancellationToken ct = default);
    Task<OrderItem> GetOrderItemByIdAsync(int id, CancellationToken ct = default);
}

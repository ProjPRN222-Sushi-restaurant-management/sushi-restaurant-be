using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class OrderItemService : IOrderItemService
{
    private readonly IOrderItemRepository _orderItemRepository;

    public OrderItemService(IOrderItemRepository orderItemRepository)
    {
        _orderItemRepository = orderItemRepository;
    }

    public Task<IReadOnlyList<OrderItem>> GetAllOrderItemsAsync(CancellationToken ct = default)
        => _orderItemRepository.GetAllOrderItemsAsync(ct);

    public Task<OrderItem> GetOrderItemByIdAsync(int id, CancellationToken ct = default)
        => _orderItemRepository.GetOrderItemByIdAsync(id, ct);
}

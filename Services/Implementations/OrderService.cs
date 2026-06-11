using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public Task<IReadOnlyList<Order>> GetAllOrdersAsync(CancellationToken ct = default)
        => _orderRepository.GetAllOrdersAsync(ct);

    public Task<Order> GetOrderByIdAsync(int id, CancellationToken ct = default)
        => _orderRepository.GetOrderById(id, ct);

    public Task AddOrderAsync(Order order, CancellationToken ct = default)
        => _orderRepository.AddOrderAsync(order, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _orderRepository.SaveChangesAsync(ct);

    public Task<IEnumerable<Order>> GetOrdersByBookingIdAsync(long bookingId)
        => _orderRepository.GetOrdersByBookingIdAsync(bookingId);
}

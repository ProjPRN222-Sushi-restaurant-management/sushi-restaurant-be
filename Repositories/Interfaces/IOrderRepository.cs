using BusinessObjects.Models;

namespace Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<IReadOnlyList<Order>> GetAllOrdersAsync(CancellationToken ct = default);
        Task<Order> GetOrderById(int id, CancellationToken ct = default);

        Task AddOrderAsync(Order order, CancellationToken ct = default);
        Task<bool> UpdateOrderAsync(Order order, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        Task<IEnumerable<Order>> GetOrdersByBookingIdAsync(long bookingId);
    }
}

using BusinessObjects.Models;

namespace Services.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllCustomersAsync(CancellationToken ct = default);
    Task<Customer?> GetCustomerByIdAsync(long id, CancellationToken ct = default);
    Task<bool> AddCustomerAsync (Customer customer, CancellationToken ct = default);
    Task<bool> DeleteCustomerAsync (long id, CancellationToken ct = default);
    Task<bool> UpdateCustomerAsync (Customer customer, CancellationToken ct = default);
    Task<bool> AdjustLoyaltyPointsAsync(long customerId, int pointDelta, CancellationToken ct = default);
}

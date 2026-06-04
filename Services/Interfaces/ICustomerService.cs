using BusinessObjects.Models;

namespace Services.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllCustomersAsync(CancellationToken ct = default);
    Task<Customer?> GetCustomerByIdAsync(long id, CancellationToken ct = default);
}

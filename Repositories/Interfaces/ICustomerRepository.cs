using BusinessObjects.Models;

namespace Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<IReadOnlyList<Customer>> GetAllCustomersAsync(CancellationToken ct = default);
        Task<Customer?> GetCustomerByIdAsync(long id, CancellationToken ct = default);
    }
}

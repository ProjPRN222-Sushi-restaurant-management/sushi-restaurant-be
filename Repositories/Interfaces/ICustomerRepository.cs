using BusinessObjects.Models;

namespace Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<IReadOnlyList<Customer>> GetAllCustomersAsync(CancellationToken ct = default);
        Task<Customer?> GetCustomerByIdAsync(long id, CancellationToken ct = default);
        Task<Customer?> GetCustomerByPhoneAsync(string phone, CancellationToken ct = default);
        Task AddCustomerAsync(Customer customer, CancellationToken ct = default);
        Task UpdateCustomerAsync(Customer customer, CancellationToken ct = default);
        Task DeleteCustomerAsync(long customerId, CancellationToken ct = default);
    }
}

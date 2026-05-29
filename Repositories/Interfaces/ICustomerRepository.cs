using BusinessObjects.Models;
using System.Reflection.Metadata;

namespace Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<IReadOnlyList<Customer>> GetAllCustomersAsync(CancellationToken ct = default);
        Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken ct = default);
    }
}

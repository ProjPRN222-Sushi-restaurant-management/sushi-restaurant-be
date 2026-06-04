using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public Task<IReadOnlyList<Customer>> GetAllCustomersAsync(CancellationToken ct = default)
        => _customerRepository.GetAllCustomersAsync(ct);

    public Task<Customer?> GetCustomerByIdAsync(long id, CancellationToken ct = default)
        => _customerRepository.GetCustomerByIdAsync(id, ct);
}

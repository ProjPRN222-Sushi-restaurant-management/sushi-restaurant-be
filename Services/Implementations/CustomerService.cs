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

    public async Task<bool> AddCustomerAsync(Customer customer, CancellationToken ct = default)
    {
        await _customerRepository.AddCustomerAsync(customer, ct);
        return true;
    }

    public async Task<bool> UpdateCustomerAsync(Customer customer, CancellationToken ct = default)
    {
        await _customerRepository.UpdateCustomerAsync(customer, ct);
        return true;
    }

    public async Task<bool> DeleteCustomerAsync(long customerId, CancellationToken ct = default)
    {
        await _customerRepository.DeleteCustomerAsync(customerId, ct);
        return true;
    }
}

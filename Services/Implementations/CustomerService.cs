using BusinessObjects.Enums;
using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Policies;

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

    public static decimal GetDiscountPercent(MembershipLevelEnum membershipLevel)
        => LoyaltyPolicy.GetDiscountPercent(membershipLevel);

    public async Task<bool> AddCustomerAsync(Customer customer, CancellationToken ct = default)
    {
        customer.Phone = customer.Phone.Trim();
        customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints);
        customer.MembershipLevel = LoyaltyPolicy.CalculateMembershipLevel(customer.LoyaltyPoints);

        var existingCustomer = await _customerRepository.GetCustomerByPhoneAsync(customer.Phone, ct);
        if (existingCustomer != null)
        {
            if (existingCustomer.DeletedAt == null)
            {
                return false;
            }

            existingCustomer.FullName = customer.FullName.Trim();
            existingCustomer.DeletedAt = null;
            existingCustomer.MembershipLevel = LoyaltyPolicy.CalculateMembershipLevel(existingCustomer.LoyaltyPoints);

            await _customerRepository.UpdateCustomerAsync(existingCustomer, ct);
            return true;
        }

        await _customerRepository.AddCustomerAsync(customer, ct);
        return true;
    }

    public async Task<bool> UpdateCustomerAsync(Customer customer, CancellationToken ct = default)
    {
        customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints);
        customer.MembershipLevel = LoyaltyPolicy.CalculateMembershipLevel(customer.LoyaltyPoints);
        await _customerRepository.UpdateCustomerAsync(customer, ct);
        return true;
    }

    public async Task<bool> DeleteCustomerAsync(long customerId, CancellationToken ct = default)
    {
        await _customerRepository.DeleteCustomerAsync(customerId, ct);
        return true;
    }

    public async Task<bool> AdjustLoyaltyPointsAsync(
        long customerId,
        int pointDelta,
        CancellationToken ct = default)
    {
        if (pointDelta == 0)
        {
            return true;
        }

        var customer = await _customerRepository.GetCustomerByIdAsync(customerId, ct);
        if (customer == null)
        {
            return false;
        }

        customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints + pointDelta);
        customer.MembershipLevel = LoyaltyPolicy.CalculateMembershipLevel(customer.LoyaltyPoints);

        await _customerRepository.UpdateCustomerAsync(customer, ct);
        return true;
    }
}

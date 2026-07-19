using BusinessObjects.Enums;
using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class CustomerService : ICustomerService
{
    private const int SilverPoints = 100;
    private const int GoldPoints = 500;
    private const int DiamondPoints = 1000;

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
        customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints);
        customer.MembershipLevel = CalculateMembershipLevel(customer.LoyaltyPoints);
        await _customerRepository.AddCustomerAsync(customer, ct);
        return true;
    }

    public async Task<bool> UpdateCustomerAsync(Customer customer, CancellationToken ct = default)
    {
        customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints);
        customer.MembershipLevel = CalculateMembershipLevel(customer.LoyaltyPoints);
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
        customer.MembershipLevel = CalculateMembershipLevel(customer.LoyaltyPoints);

        await _customerRepository.UpdateCustomerAsync(customer, ct);
        return true;
    }

    private static MembershipLevelEnum CalculateMembershipLevel(int loyaltyPoints)
    {
        return loyaltyPoints switch
        {
            >= DiamondPoints => MembershipLevelEnum.DIAMOND,
            >= GoldPoints => MembershipLevelEnum.GOLD,
            >= SilverPoints => MembershipLevelEnum.SILVER,
            _ => MembershipLevelEnum.NONE
        };
    }
}

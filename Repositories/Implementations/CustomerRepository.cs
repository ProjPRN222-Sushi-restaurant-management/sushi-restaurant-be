using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class CustomerRepository : ICustomerRepository 
    {
        private readonly RestaurantSystemDbContext _context;

        public CustomerRepository(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Customer>> GetAllCustomersAsync(
            CancellationToken ct = default)
        {
            return await _context.Customers
                .AsNoTracking()
                .OrderBy(c => c.CustomerId)
                .ToListAsync(ct);
        }

        public async Task<Customer?> GetCustomerByIdAsync(
            long id,
            CancellationToken ct = default)
        {
            return await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == id, ct);
        }

        public async Task<Customer?> GetCustomerByPhoneAsync(string phone, CancellationToken ct = default)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone, ct);
        }

        public async Task AddCustomerAsync(Customer customer, CancellationToken ct = default)
        {
            await _context.Customers.AddAsync(customer, ct);
        }
    }
}

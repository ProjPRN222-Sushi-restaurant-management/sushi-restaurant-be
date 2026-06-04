using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class StaffRepository : IStaffRepository
    {
        private readonly RestaurantSystemDbContext _context;

        public StaffRepository(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Staff>> GetAllStaffsAsync(
            CancellationToken ct = default)
        {
            return await _context.Staffs
                .AsNoTracking()
                .OrderBy(s => s.StaffId)
                .ToListAsync(ct);
        }

        public async Task<Staff?> GetStaffById(
            int id,
            CancellationToken ct = default)
        {
            return await _context.Staffs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StaffId == id, ct);
        }
    }
}  

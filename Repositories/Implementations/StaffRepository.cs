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
            long id,
            CancellationToken ct = default)
        {
            return await _context.Staffs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StaffId == id, ct);
        }

        public async Task<Staff?> GetStaffByPhoneAsync(
            string phone,
            CancellationToken ct = default)
        {
            return await _context.Staffs
                .FirstOrDefaultAsync(s => s.Phone == phone, ct);
        }

        public async Task<bool> AddStaffAsync(
            Staff staff,
            CancellationToken ct = default)
        {
            if (staff == null) return false;
            await _context.Staffs.AddAsync(staff, ct);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateStaffAsync(
            Staff staff,
            CancellationToken ct = default)
        {
            if (staff == null) return false;
            _context.Staffs.Update(staff);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteStaffAsync(
            long id,
            CancellationToken ct = default)
        {
            var staff = await _context.Staffs.FindAsync(new object[] { id }, ct);
            if (staff == null) return false;
            _context.Staffs.Remove(staff);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}  

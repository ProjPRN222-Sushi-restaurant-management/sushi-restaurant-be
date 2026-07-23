using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class StaffService : IStaffService
{
    private readonly IStaffRepository _staffRepository;

    public StaffService(IStaffRepository staffRepository)
    {
        _staffRepository = staffRepository;
    }

    public Task<IReadOnlyList<Staff>> GetAllStaffsAsync(CancellationToken ct = default)
        => _staffRepository.GetAllStaffsAsync(ct);

    public Task<Staff?> GetStaffByIdAsync(long id, CancellationToken ct = default)
        => _staffRepository.GetStaffById(id, ct);

    public async Task<bool> AddStaffAsync(Staff staff, CancellationToken ct = default)
    {
        if (staff == null || string.IsNullOrWhiteSpace(staff.Phone))
        {
            return false;
        }

        try
        {
            staff.Phone = staff.Phone.Trim();
            var existingStaff = await _staffRepository.GetStaffByPhoneAsync(staff.Phone, ct);

            if (existingStaff != null)
            {
                if (existingStaff.DeletedAt == null && existingStaff.IsActive)
                {
                    return false;
                }

                existingStaff.FullName = staff.FullName.Trim();
                existingStaff.PasswordHash = staff.PasswordHash;
                existingStaff.IsActive = true;
                existingStaff.DeletedAt = null;

                await _staffRepository.UpdateStaffAsync(existingStaff, ct);
                return true;
            }

            await _staffRepository.AddStaffAsync(staff, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateStaffAsync(Staff staff, CancellationToken ct = default)
    {
        if (staff == null || staff.StaffId <= 0)
        {
            return false;
        }

        try
        {
            await _staffRepository.UpdateStaffAsync(staff, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteStaffAsync(long id, CancellationToken ct = default)
    {
        try
        {
            return await _staffRepository.DeleteStaffAsync(id, ct);
        }
        catch
        {
            return false;
        }
    }
}

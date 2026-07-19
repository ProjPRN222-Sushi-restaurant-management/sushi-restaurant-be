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
        if (staff == null || string.IsNullOrWhiteSpace(staff.Phone)) return false;
        try
        {
            await _staffRepository.AddStaffAsync(staff, ct); // Ho?c tên hàm Add t??ng ?ng ? Repo c?a b?n
            return true;
        }
        catch
        {
            return false;
        }
    }

    // 2. EDIT
    public async Task<bool> UpdateStaffAsync(Staff staff, CancellationToken ct = default)
    {
        if (staff == null || staff.StaffId <= 0) return false;
        try
        {
            await _staffRepository.UpdateStaffAsync(staff, ct); // Ho?c tên hàm Update t??ng ?ng ? Repo c?a b?n
            return true;
        }
        catch
        {
            return false;
        }
    }

    // 3. XÓA NHÂN VIÊN (DELETE)
    public async Task<bool> DeleteStaffAsync(long id, CancellationToken ct = default)
    {
        try
        {
            return await _staffRepository.DeleteStaffAsync(id, ct); // Ho?c tên hàm Delete t??ng ?ng ? Repo c?a b?n
        }
        catch
        {
            return false;
        }
    }
}

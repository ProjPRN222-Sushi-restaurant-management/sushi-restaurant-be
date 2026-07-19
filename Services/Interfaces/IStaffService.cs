using BusinessObjects.Models;

namespace Services.Interfaces;

public interface IStaffService
{
    Task<IReadOnlyList<Staff>> GetAllStaffsAsync(CancellationToken ct = default);
    Task<Staff?> GetStaffByIdAsync(long id, CancellationToken ct = default);
    Task<bool> AddStaffAsync(Staff staff, CancellationToken ct = default);
    Task<bool> UpdateStaffAsync(Staff staff, CancellationToken ct = default);
    Task<bool> DeleteStaffAsync(long id, CancellationToken ct = default);
}

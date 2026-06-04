using BusinessObjects.Models;

namespace Services.Interfaces;

public interface IStaffService
{
    Task<IReadOnlyList<Staff>> GetAllStaffsAsync(CancellationToken ct = default);
    Task<Staff?> GetStaffByIdAsync(int id, CancellationToken ct = default);
}

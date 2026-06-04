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

    public Task<Staff?> GetStaffByIdAsync(int id, CancellationToken ct = default)
        => _staffRepository.GetStaffById(id, ct);
}

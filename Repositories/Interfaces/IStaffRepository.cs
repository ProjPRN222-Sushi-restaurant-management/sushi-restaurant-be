using BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IStaffRepository
    {
        Task<IReadOnlyList<Staff>> GetAllStaffsAsync(CancellationToken ct = default);
        Task<Staff?> GetStaffById(int id, CancellationToken ct = default);
    }
}

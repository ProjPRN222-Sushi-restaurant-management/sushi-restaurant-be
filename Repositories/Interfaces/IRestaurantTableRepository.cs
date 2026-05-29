using BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
     public interface IRestaurantTableRepository
    {
        Task<IReadOnlyList<RestaurantTable>> GetAllTablesAsync(CancellationToken ct = default);
        Task<RestaurantTable> GetTableById(int id, CancellationToken ct = default);
    }
}

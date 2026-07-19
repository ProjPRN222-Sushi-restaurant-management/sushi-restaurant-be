using BusinessObjects.Models;

namespace Services.Interfaces;

public interface IRestaurantTableService
{
    Task<IReadOnlyList<RestaurantTable>> GetAllTablesAsync(CancellationToken ct = default);
    Task<RestaurantTable> GetTableByIdAsync(int id, CancellationToken ct = default);
}

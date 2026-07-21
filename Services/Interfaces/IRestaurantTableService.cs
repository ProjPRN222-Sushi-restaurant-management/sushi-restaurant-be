using BusinessObjects.Models;
using BusinessObjects.Enums;

namespace Services.Interfaces;

public interface IRestaurantTableService
{
    Task<IReadOnlyList<RestaurantTable>> GetAllTablesAsync(CancellationToken ct = default);
    Task<RestaurantTable> GetTableByIdAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateTableStatusAsync(long tableId, TableStatusEnum status, CancellationToken ct = default);
}

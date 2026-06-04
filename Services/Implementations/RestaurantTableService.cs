using BusinessObjects.Models;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class RestaurantTableService : IRestaurantTableService
{
    private readonly IRestaurantTableRepository _restaurantTableRepository;

    public RestaurantTableService(IRestaurantTableRepository restaurantTableRepository)
    {
        _restaurantTableRepository = restaurantTableRepository;
    }

    public Task<IReadOnlyList<RestaurantTable>> GetAllTablesAsync(CancellationToken ct = default)
        => _restaurantTableRepository.GetAllTablesAsync(ct);

    public Task<RestaurantTable> GetTableByIdAsync(int id, CancellationToken ct = default)
        => _restaurantTableRepository.GetTableById(id, ct);
}

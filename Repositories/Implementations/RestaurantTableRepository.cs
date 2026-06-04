using BusinessObjects.Models;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class RestaurantTableRepository : IRestaurantTableRepository
    {
        private readonly RestaurantSystemDbContext _context;

        public RestaurantTableRepository(RestaurantSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<RestaurantTable>> GetAllTablesAsync(
            CancellationToken ct = default)
        {
            return await _context.RestaurantTables
                .AsNoTracking()
                .OrderBy(t => t.TableId)
                .ToListAsync(ct);
        }

        public async Task<RestaurantTable> GetTableById(
            int id,
            CancellationToken ct = default)
        {
            var table = await _context.RestaurantTables
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableId == id, ct);

            return table ?? throw new KeyNotFoundException($"Table {id} not found.");
        }
    }
}

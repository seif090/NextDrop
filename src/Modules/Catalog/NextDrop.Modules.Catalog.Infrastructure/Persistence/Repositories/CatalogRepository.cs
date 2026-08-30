using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Catalog.Domain.Enums;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Catalog.Infrastructure.Persistence.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private readonly NextDropDbContext _context;

    public CatalogRepository(NextDropDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Aggregates.Catalog?> GetByIdAsync(CatalogId id, CancellationToken cancellationToken = default)
    {
        return await _context.Catalogs
            .Include(c => c.Categories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Domain.Aggregates.Catalog?> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _context.Catalogs
            .Include(c => c.Categories)
            .FirstOrDefaultAsync(c => c.RestaurantId == restaurantId && c.Status != CatalogStatus.Archived, cancellationToken);
    }

    public async Task<Domain.Aggregates.Catalog?> GetPublishedByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default)
    {
        return await _context.Catalogs
            .Include(c => c.Categories)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.RestaurantId == restaurantId && c.Status == CatalogStatus.Published, cancellationToken);
    }

    public async Task AddAsync(Domain.Aggregates.Catalog catalog, CancellationToken cancellationToken = default)
    {
        await _context.Catalogs.AddAsync(catalog, cancellationToken);
    }
}

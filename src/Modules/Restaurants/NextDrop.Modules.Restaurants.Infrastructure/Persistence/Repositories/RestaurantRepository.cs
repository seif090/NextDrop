using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Restaurants.Application.Abstractions;
using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.Enums;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Restaurants.Infrastructure.Persistence.Repositories;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly NextDropDbContext _context;

    public RestaurantRepository(NextDropDbContext context)
    {
        _context = context;
    }

    public async Task<Restaurant?> GetByIdAsync(RestaurantId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Restaurant>()
            .Include(r => r.Branches)
                .ThenInclude(b => b.OperatingHours)
            .Include(r => r.Branches)
                .ThenInclude(b => b.DeliveryZones)
            .Include(r => r.StaffMemberships)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Restaurant?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Restaurant>()
            .Include(r => r.Branches)
                .ThenInclude(b => b.OperatingHours)
            .Include(r => r.Branches)
                .ThenInclude(b => b.DeliveryZones)
            .Include(r => r.StaffMemberships)
            .FirstOrDefaultAsync(r => r.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        await _context.Set<Restaurant>().AddAsync(restaurant, cancellationToken);
    }

    public void Update(Restaurant restaurant)
    {
        // Entity graph change tracking managed by EF Core
    }

    public async Task<(IReadOnlyList<Restaurant> Items, int TotalCount)> GetPublicPagedAsync(
        int page,
        int pageSize,
        string? cityFilter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Restaurant>()
            .AsNoTracking()
            .Include(r => r.Branches)
            .Where(r => r.Status == RestaurantStatus.Active);

        if (!string.IsNullOrWhiteSpace(cityFilter))
        {
            var normalizedCity = cityFilter.Trim().ToLowerInvariant();
            query = query.Where(r => r.Branches.Any(b => b.Status == BranchStatus.Active && b.City.ToLower() == normalizedCity));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

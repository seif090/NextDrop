using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Catalog.Domain.Entities;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Catalog.Infrastructure.Persistence.Repositories;

public class BranchMenuItemAvailabilityRepository : IBranchMenuItemAvailabilityRepository
{
    private readonly NextDropDbContext _context;

    public BranchMenuItemAvailabilityRepository(NextDropDbContext context)
    {
        _context = context;
    }

    public async Task<BranchMenuItemAvailability?> GetAsync(MenuItemId menuItemId, RestaurantBranchId branchId, CancellationToken cancellationToken = default)
    {
        return await _context.BranchMenuItemAvailabilities
            .FirstOrDefaultAsync(b => b.MenuItemId == menuItemId && b.RestaurantBranchId == branchId, cancellationToken);
    }

    public async Task AddAsync(BranchMenuItemAvailability availability, CancellationToken cancellationToken = default)
    {
        await _context.BranchMenuItemAvailabilities.AddAsync(availability, cancellationToken);
    }
}

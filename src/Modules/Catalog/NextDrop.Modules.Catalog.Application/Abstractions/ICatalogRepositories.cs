using NextDrop.Modules.Catalog.Domain.Aggregates;
using NextDrop.Modules.Catalog.Domain.Entities;
using NextDrop.Modules.Catalog.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Catalog.Application.Abstractions;

public interface ICatalogRepository
{
    Task<Domain.Aggregates.Catalog?> GetByIdAsync(CatalogId id, CancellationToken cancellationToken = default);
    Task<Domain.Aggregates.Catalog?> GetByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task<Domain.Aggregates.Catalog?> GetPublishedByRestaurantIdAsync(RestaurantId restaurantId, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Aggregates.Catalog catalog, CancellationToken cancellationToken = default);
}

public interface IMenuItemRepository
{
    Task<MenuItem?> GetByIdAsync(MenuItemId id, CancellationToken cancellationToken = default);
    Task<List<MenuItem>> GetByCatalogIdAsync(CatalogId catalogId, CancellationToken cancellationToken = default);
    Task<int> GetCountByCatalogIdAsync(CatalogId catalogId, CancellationToken cancellationToken = default);
    Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken = default);
}

public interface IBranchMenuItemAvailabilityRepository
{
    Task<BranchMenuItemAvailability?> GetAsync(MenuItemId menuItemId, RestaurantBranchId branchId, CancellationToken cancellationToken = default);
    Task AddAsync(BranchMenuItemAvailability availability, CancellationToken cancellationToken = default);
}

public interface ICatalogCacheService
{
    Task<DTOs.PublicCatalogDto?> GetPublicCatalogAsync(Guid restaurantId, CancellationToken cancellationToken = default);
    Task SetPublicCatalogAsync(Guid restaurantId, DTOs.PublicCatalogDto catalog, CancellationToken cancellationToken = default);
    Task InvalidatePublicCatalogAsync(Guid restaurantId, CancellationToken cancellationToken = default);
}

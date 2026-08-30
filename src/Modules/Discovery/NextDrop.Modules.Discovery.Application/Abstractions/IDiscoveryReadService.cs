using NextDrop.Modules.Discovery.Application.DTOs;
using NextDrop.Modules.Discovery.Domain.ValueObjects;

namespace NextDrop.Modules.Discovery.Application.Abstractions;

public interface IDiscoveryReadService
{
    Task<PagedDiscoveryResultDto<PublicRestaurantDto>> GetPublicRestaurantsAsync(RestaurantDiscoveryCriteria criteria, CancellationToken cancellationToken = default);
    Task<PublicRestaurantDto?> GetPublicRestaurantByIdAsync(Guid restaurantId, CancellationToken cancellationToken = default);
    Task<List<PublicBranchDto>> GetPublicRestaurantBranchesAsync(Guid restaurantId, CancellationToken cancellationToken = default);
    Task<PagedDiscoveryResultDto<PublicMenuItemDto>> GetPublicMenuItemsAsync(MenuItemDiscoveryCriteria criteria, CancellationToken cancellationToken = default);
}

public interface IDiscoveryCacheService
{
    Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string cacheKey, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}

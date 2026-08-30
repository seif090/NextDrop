using NextDrop.Modules.Restaurants.Domain.Aggregates;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Restaurants.Application.Abstractions;

public interface IRestaurantRepository
{
    Task<Restaurant?> GetByIdAsync(RestaurantId id, CancellationToken cancellationToken = default);
    Task<Restaurant?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Restaurant restaurant, CancellationToken cancellationToken = default);
    void Update(Restaurant restaurant);
    Task<(IReadOnlyList<Restaurant> Items, int TotalCount)> GetPublicPagedAsync(int page, int pageSize, string? cityFilter, CancellationToken cancellationToken = default);
}

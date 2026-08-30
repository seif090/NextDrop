using NextDrop.Modules.Delivery.Application.DTOs;
using NextDrop.Modules.Delivery.Domain.Aggregates;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.ValueObjects;

namespace NextDrop.Modules.Delivery.Application.Abstractions;

public interface IRiderRepository
{
    Task<Rider?> GetByIdAsync(RiderId id, CancellationToken cancellationToken = default);
    Task<Rider?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Rider>> GetAvailableRidersAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Rider rider, CancellationToken cancellationToken = default);
}

public interface IDeliveryRepository
{
    Task<Domain.Aggregates.Delivery?> GetByIdAsync(DeliveryId id, CancellationToken cancellationToken = default);
    Task<Domain.Aggregates.Delivery?> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);
    Task<Domain.Aggregates.Delivery?> GetActiveDeliveryByRiderIdAsync(RiderId riderId, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Aggregates.Delivery delivery, CancellationToken cancellationToken = default);
}

public interface IDistanceCalculator
{
    double CalculateDistanceKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2);
}

public interface IRiderLocationCacheService
{
    Task SetLocationAsync(Guid riderId, LocationDto location, CancellationToken cancellationToken = default);
    Task<LocationDto?> GetLocationAsync(Guid riderId, CancellationToken cancellationToken = default);
}

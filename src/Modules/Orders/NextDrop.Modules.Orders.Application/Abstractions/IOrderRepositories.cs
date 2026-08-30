using NextDrop.Modules.Customers.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.Aggregates;
using NextDrop.Modules.Orders.Domain.ValueObjects;
using NextDrop.Modules.Restaurants.Domain.ValueObjects;

namespace NextDrop.Modules.Orders.Application.Abstractions;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(CartId id, CancellationToken cancellationToken = default);
    Task<Cart?> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);
    void Delete(Cart cart);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<(List<Order> Items, int TotalCount)> GetPagedByCustomerIdAsync(CustomerId customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(List<Order> Items, int TotalCount)> GetPagedByRestaurantIdAsync(RestaurantId restaurantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}

public interface IOrderNumberGenerator
{
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);
}

public interface ICartCacheService
{
    Task<DTOs.CartDto?> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task SetCartAsync(Guid customerId, DTOs.CartDto cart, CancellationToken cancellationToken = default);
    Task InvalidateCartAsync(Guid customerId, CancellationToken cancellationToken = default);
}

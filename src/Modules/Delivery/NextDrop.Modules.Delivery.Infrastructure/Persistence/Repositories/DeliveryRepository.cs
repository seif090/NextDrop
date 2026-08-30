using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Delivery.Application.Abstractions;
using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.ValueObjects;
using NextDrop.Modules.Orders.Domain.ValueObjects;

namespace NextDrop.Modules.Delivery.Infrastructure.Persistence.Repositories;

public class DeliveryRepository : IDeliveryRepository
{
    private readonly NextDropDbContext _dbContext;

    public DeliveryRepository(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Domain.Aggregates.Delivery?> GetByIdAsync(DeliveryId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Deliveries
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Domain.Aggregates.Delivery?> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Deliveries
            .FirstOrDefaultAsync(d => d.OrderId == orderId, cancellationToken);
    }

    public async Task<Domain.Aggregates.Delivery?> GetActiveDeliveryByRiderIdAsync(RiderId riderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Deliveries
            .FirstOrDefaultAsync(d => d.RiderId == riderId &&
                                      d.Status != DeliveryStatus.Delivered &&
                                      d.Status != DeliveryStatus.Failed &&
                                      d.Status != DeliveryStatus.Cancelled, cancellationToken);
    }

    public async Task AddAsync(Domain.Aggregates.Delivery delivery, CancellationToken cancellationToken = default)
    {
        await _dbContext.Deliveries.AddAsync(delivery, cancellationToken);
    }
}

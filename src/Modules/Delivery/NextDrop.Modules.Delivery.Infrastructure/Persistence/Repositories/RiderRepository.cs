using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Delivery.Application.Abstractions;
using NextDrop.Modules.Delivery.Domain.Aggregates;
using NextDrop.Modules.Delivery.Domain.Enums;
using NextDrop.Modules.Delivery.Domain.ValueObjects;

namespace NextDrop.Modules.Delivery.Infrastructure.Persistence.Repositories;

public class RiderRepository : IRiderRepository
{
    private readonly NextDropDbContext _dbContext;

    public RiderRepository(NextDropDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Rider?> GetByIdAsync(RiderId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Riders
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Rider?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Riders
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);
    }

    public async Task<List<Rider>> GetAvailableRidersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Riders
            .Where(r => r.Status == RiderStatus.Active && r.AvailabilityStatus == RiderAvailabilityStatus.Available)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Rider rider, CancellationToken cancellationToken = default)
    {
        await _dbContext.Riders.AddAsync(rider, cancellationToken);
    }
}

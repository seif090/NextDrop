using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NextDrop.Modules.Delivery.Application.Abstractions;
using NextDrop.Modules.Delivery.Application.DTOs;

namespace NextDrop.Modules.Delivery.Infrastructure.Services;

public class RiderLocationCacheService : IRiderLocationCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RiderLocationCacheService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public RiderLocationCacheService(IDistributedCache cache, ILogger<RiderLocationCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private static string GetCacheKey(Guid riderId) => $"rider:{riderId}:location";

    public async Task SetLocationAsync(Guid riderId, LocationDto location, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(location);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            };
            await _cache.SetStringAsync(GetCacheKey(riderId), json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write ephemeral location for rider {RiderId} to Redis.", riderId);
        }
    }

    public async Task<LocationDto?> GetLocationAsync(Guid riderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(GetCacheKey(riderId), cancellationToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<LocationDto>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read ephemeral location for rider {RiderId} from Redis.", riderId);
            return null;
        }
    }
}

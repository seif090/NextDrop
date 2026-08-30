using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NextDrop.Modules.Catalog.Application.Abstractions;
using NextDrop.Modules.Catalog.Application.DTOs;

namespace NextDrop.Modules.Catalog.Infrastructure.Services;

public class CatalogCacheService : ICatalogCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CatalogCacheService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public CatalogCacheService(IDistributedCache cache, ILogger<CatalogCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private static string GetCacheKey(Guid restaurantId) => $"catalog:public:{restaurantId}";

    public async Task<PublicCatalogDto?> GetPublicCatalogAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(GetCacheKey(restaurantId), cancellationToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<PublicCatalogDto>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache lookup failed for restaurant {RestaurantId}. Falling back to database.", restaurantId);
            return null;
        }
    }

    public async Task SetPublicCatalogAsync(Guid restaurantId, PublicCatalogDto catalog, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(catalog);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            };

            await _cache.SetStringAsync(GetCacheKey(restaurantId), json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache public catalog for restaurant {RestaurantId}.", restaurantId);
        }
    }

    public async Task InvalidatePublicCatalogAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(GetCacheKey(restaurantId), cancellationToken);
            _logger.LogInformation("Invalidated public catalog cache for restaurant {RestaurantId}.", restaurantId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate public catalog cache for restaurant {RestaurantId}.", restaurantId);
        }
    }
}

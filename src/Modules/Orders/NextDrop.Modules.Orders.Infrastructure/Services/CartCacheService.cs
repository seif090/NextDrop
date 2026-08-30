using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NextDrop.Modules.Orders.Application.Abstractions;
using NextDrop.Modules.Orders.Application.DTOs;

namespace NextDrop.Modules.Orders.Infrastructure.Services;

public class CartCacheService : ICartCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CartCacheService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    public CartCacheService(IDistributedCache cache, ILogger<CartCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private static string GetCacheKey(Guid customerId) => $"cart:customer:{customerId}";

    public async Task<CartDto?> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(GetCacheKey(customerId), cancellationToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<CartDto>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cart cache lookup failed for customer {CustomerId}. Falling back to database.", customerId);
            return null;
        }
    }

    public async Task SetCartAsync(Guid customerId, CartDto cart, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(cart);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            };

            await _cache.SetStringAsync(GetCacheKey(customerId), json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache cart for customer {CustomerId}.", customerId);
        }
    }

    public async Task InvalidateCartAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(GetCacheKey(customerId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cart cache for customer {CustomerId}.", customerId);
        }
    }
}

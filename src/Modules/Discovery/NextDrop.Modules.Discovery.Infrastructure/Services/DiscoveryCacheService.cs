using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NextDrop.SharedKernel.Abstractions;
using NextDrop.Modules.Discovery.Application.Abstractions;

namespace NextDrop.Modules.Discovery.Infrastructure.Services;

public class DiscoveryCacheService : IDiscoveryCacheService
{
    private readonly ICacheService _cacheService;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<DiscoveryCacheService>? _logger;

    public DiscoveryCacheService(ICacheService cacheService, IDistributedCache? distributedCache = null, ILogger<DiscoveryCacheService>? logger = null)
    {
        _cacheService = cacheService;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string cacheKey, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _cacheService.GetAsync<T>(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read from cache for key {CacheKey}", cacheKey);
            return default;
        }
    }

    public async Task SetAsync<T>(string cacheKey, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.SetAsync(cacheKey, value, ttl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to write to cache for key {CacheKey}", cacheKey);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_cacheService != null)
            {
                await _cacheService.RemoveAsync(prefix, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to remove cache key by prefix {Prefix}", prefix);
        }
    }
}

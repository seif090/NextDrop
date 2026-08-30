using System.Collections.Concurrent;
using NextDrop.SharedKernel.Abstractions;

namespace NextDrop.Infrastructure.Services;

public class InMemoryIdempotencyService : IIdempotencyService
{
    private sealed record CacheEntry(string RequestHash, IdempotencyResponse Response, DateTimeOffset ExpiresAtUtc);

    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

    public Task<bool> IsKeyProcessedAsync(string key, string requestHash, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.ContainsKey(key));
    }

    public Task<IdempotencyResponse?> GetCachedResponseAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var entry) && entry.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            return Task.FromResult<IdempotencyResponse?>(entry.Response);
        }

        return Task.FromResult<IdempotencyResponse?>(null);
    }

    public Task CacheResponseAsync(
        string key,
        string requestHash,
        int statusCode,
        string contentType,
        string body,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(expiration ?? TimeSpan.FromHours(24));
        var response = new IdempotencyResponse(statusCode, contentType, body);
        _store[key] = new CacheEntry(requestHash, response, expiresAt);
        return Task.CompletedTask;
    }

    public bool IsPayloadMismatch(string key, string requestHash)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            return !string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal);
        }
        return false;
    }
}

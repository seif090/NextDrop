namespace NextDrop.SharedKernel.Abstractions;

public record IdempotencyResponse(int StatusCode, string ContentType, string Body);

public interface IIdempotencyService
{
    Task<bool> IsKeyProcessedAsync(string key, string requestHash, CancellationToken cancellationToken = default);
    Task<IdempotencyResponse?> GetCachedResponseAsync(string key, CancellationToken cancellationToken = default);
    Task CacheResponseAsync(string key, string requestHash, int statusCode, string contentType, string body, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
}

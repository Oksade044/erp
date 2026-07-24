using ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ERP.Infrastructure.Caching;

/// <summary>
/// ICacheService-in lokal (in-process) implementasiyası — IMemoryCache (TDD §37).
/// Default TTL konfiqurasiyadan (Cache:DefaultTtlSeconds).
/// </summary>
public sealed class MemoryCacheService(IMemoryCache cache, IConfiguration configuration) : ICacheService
{
    private readonly TimeSpan _defaultTtl =
        TimeSpan.FromSeconds(configuration.GetValue("Cache:DefaultTtlSeconds", 60));

    public async Task<T> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory(ct);
        cache.Set(key, value, ttl ?? _defaultTtl);
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}

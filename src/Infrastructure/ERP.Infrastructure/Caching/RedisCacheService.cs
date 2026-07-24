using System.Text.Json;
using ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace ERP.Infrastructure.Caching;

/// <summary>
/// ICacheService-in Redis implementasiyası — IDistributedCache üzərindən (TDD §37).
/// Paylaşılan keş: node-lar arası eyni. Dəyər JSON kimi serialize olunur. Kod eyni interfeyslə
/// işlədiyi üçün Memory→Redis keçidi yalnız konfiqurasiyadır.
/// </summary>
public sealed class RedisCacheService(IDistributedCache cache, IConfiguration configuration) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly TimeSpan _defaultTtl =
        TimeSpan.FromSeconds(configuration.GetValue("Cache:DefaultTtlSeconds", 60));

    public async Task<T> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        if (bytes is not null)
        {
            var cached = JsonSerializer.Deserialize<T>(bytes, JsonOpts);
            if (cached is not null) return cached;
        }

        var value = await factory(ct);
        await cache.SetAsync(
            key,
            JsonSerializer.SerializeToUtf8Bytes(value, JsonOpts),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl },
            ct);
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) => cache.RemoveAsync(key, ct);
}

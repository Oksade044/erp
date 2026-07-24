namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Keş abstraksiyası (TDD §37). Lokalda IMemoryCache, serverdə Redis — keçid konfiqurasiya ilə,
/// kod dəyişmir. Dəyişməyən/ağır data keşlənir; dəyişəndə RemoveAsync ilə etibarsızlaşdırılır.
/// </summary>
public interface ICacheService
{
    /// <summary>Keşdə varsa qaytarır; yoxdursa factory çağırır, saxlayır və qaytarır.</summary>
    Task<T> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>Keş açarını silir (etibarsızlaşdırma).</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);
}

/// <summary>Keş açarları (magic string yox, mərkəzləşdirilmiş).</summary>
public static class CacheKeys
{
    public const string Dashboard = "reports:dashboard";
}

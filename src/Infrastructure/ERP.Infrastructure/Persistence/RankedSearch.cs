using ERP.Application.Common.Models;
using ERP.Domain.Common;

namespace ERP.Infrastructure.Persistence;

/// <summary>
/// Bütün repozitoriyalar üçün ortaq axtarış+səhifələmə (DRY). Yaddaşdakı siyahını
/// diakritiksiz, böyük/kiçik hərfə həssas olmayan, sıralanmış (tam→söz əvvəli→daxil)
/// axtarışla süzür (bax: <see cref="SearchNormalizer"/>). Axtarış boşdursa əlifba sırası.
/// </summary>
public static class RankedSearch
{
    public static PagedResult<T> Page<T>(
        IReadOnlyList<T> source, string? search, int page, int pageSize,
        Func<T, string?> primary, Func<T, IEnumerable<string?>>? secondary = null)
    {
        IReadOnlyList<T> matched;

        if (string.IsNullOrWhiteSpace(search))
        {
            matched = source
                .OrderBy(primary, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            var term = SearchNormalizer.Normalize(search);
            matched = source
                .Select(x => (x, rank: SearchNormalizer.Score(term, primary(x), secondary?.Invoke(x))))
                .Where(t => t.rank != SearchNormalizer.NoMatch)
                .OrderBy(t => t.rank)                                    // tam→söz əvvəli→daxil
                .ThenBy(t => (primary(t.x) ?? string.Empty).Length)
                .ThenBy(t => primary(t.x), StringComparer.OrdinalIgnoreCase)
                .Select(t => t.x)
                .ToList();
        }

        var items = matched.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<T> { Items = items, TotalCount = matched.Count, Page = page, PageSize = pageSize };
    }
}

namespace ERP.Application.Common.Models;

/// <summary>
/// Server-side pagination nəticəsi (TDD §11). Böyük siyahılar heç vaxt tam
/// qaytarılmır — həmişə səhifələnir.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

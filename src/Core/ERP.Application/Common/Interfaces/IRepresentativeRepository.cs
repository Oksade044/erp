using ERP.Domain.Modules.Representatives;

namespace ERP.Application.Common.Interfaces;

/// <summary>Təmsilçi defter qeydlərinə xas repository (#16-18).</summary>
public interface IRepresentativeRepository : IRepository<RepresentativeEntry>
{
    /// <summary>Bir təmsilçinin bütün qeydləri (ən yeni əvvəl).</summary>
    Task<IReadOnlyList<RepresentativeEntry>> GetByRepresentativeAsync(string name, CancellationToken ct = default);

    /// <summary>Bütün qeydlər (balans xülasəsi üçün, təmsilçi filtri olmadan).</summary>
    Task<IReadOnlyList<RepresentativeEntry>> GetAllAsync(CancellationToken ct = default);
}

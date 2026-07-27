using ERP.Domain.Modules.Suppliers;

namespace ERP.Application.Common.Interfaces;

/// <summary>Təchizatçı defteri qeydlərinə xas repository (#15).</summary>
public interface ISupplierLedgerRepository : IRepository<SupplierLedgerEntry>
{
    /// <summary>Bir təchizatçının bütün defter qeydləri (ən yeni əvvəl).</summary>
    Task<IReadOnlyList<SupplierLedgerEntry>> GetBySupplierAsync(Guid supplierId, CancellationToken ct = default);

    Task<SupplierLedgerEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

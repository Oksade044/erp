using ERP.Application.Common.Models;
using ERP.Domain.Modules.Invoices;

namespace ERP.Application.Common.Interfaces;

/// <summary>Fakturaya xas repository (TDD §14).</summary>
public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByIdWithPaymentsAsync(Guid id, CancellationToken ct = default);
    /// <summary>Sifarişə görə fakturanı (ödənişlərlə, tracked) qaytarır.</summary>
    Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Mövcud (tracked) fakturaya əlavə edilmiş yeni ödənişi açıq şəkildə "Added" kimi izləyir.
    /// Client-set Guid açarları səbəbindən EF yeni uşaq entity-ni default halda "Modified" sayır
    /// (yenilənmə → 0 sətir → concurrency xətası). Bu, onu düzgün INSERT edir.
    /// </summary>
    void AttachNewPayment(Payment payment);

    Task<PagedResult<Invoice>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
}

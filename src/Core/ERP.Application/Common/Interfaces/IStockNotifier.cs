using ERP.Shared.Contracts.Warehouses;

namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Canlı stok bildirişləri üçün abstraksiya (TDD §38). Application real-time texnologiyasını
/// (SignalR) tanımır — implementasiya API/host layer-də verilir (asılılıq içəriyə, TDD §8).
/// </summary>
public interface IStockNotifier
{
    Task NotifyStockChangedAsync(StockChangedNotification notification, CancellationToken ct = default);
}

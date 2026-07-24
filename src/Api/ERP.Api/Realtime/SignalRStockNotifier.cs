using ERP.Application.Common.Interfaces;
using ERP.Shared.Contracts.Warehouses;
using Microsoft.AspNetCore.SignalR;

namespace ERP.Api.Realtime;

/// <summary>
/// IStockNotifier-in SignalR implementasiyası (TDD §38). Stok dəyişikliyini bütün qoşulu
/// klientlərə "StockChanged" hadisəsi kimi yayımlayır.
/// </summary>
public sealed class SignalRStockNotifier(IHubContext<StockHub> hub) : IStockNotifier
{
    public Task NotifyStockChangedAsync(StockChangedNotification notification, CancellationToken ct = default) =>
        hub.Clients.All.SendAsync("StockChanged", notification, ct);
}

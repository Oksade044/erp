using Microsoft.AspNetCore.SignalR;

namespace ERP.Api.Realtime;

/// <summary>
/// Canlı stok hub-ı (TDD §38). Klientlər qoşulur və "StockChanged" hadisəsini alırlar.
/// Serverdən klientə tək istiqamətli yayım — klient metod çağırmır.
/// Lokalda AllowAnonymous (API-first, lokal PC); serverdə token-auth əlavə oluna bilər.
/// </summary>
public sealed class StockHub : Hub
{
}

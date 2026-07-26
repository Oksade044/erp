namespace ERP.Domain.Modules.Orders;

/// <summary>
/// Sifariş növü (#33). İcarə — müvəqqəti (tarix, depozit, qaytarma);
/// Satış — birdəfəlik (qaytarma/depozit yox, stok daimi çıxılır).
/// </summary>
public enum OrderType
{
    İcarə = 1,
    Satış = 2
}

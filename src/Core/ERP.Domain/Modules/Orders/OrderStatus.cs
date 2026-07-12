namespace ERP.Domain.Modules.Orders;

/// <summary>
/// İcarə sifarişinin statusu (həyat dövrü).
/// Qaralama → Təsdiqlənmiş → TəhvilVerilmiş → Qaytarılmış. İstənilən mərhələdə Ləğv (məhdud).
/// Yalnız Təsdiqlənmiş/TəhvilVerilmiş sifarişlər anbarı rezerv edir (ikiqat-bron yoxlaması).
/// </summary>
public enum OrderStatus
{
    Qaralama = 1,
    Təsdiqlənmiş = 2,
    TəhvilVerilmiş = 3,
    Qaytarılmış = 4,
    Ləğv = 5
}

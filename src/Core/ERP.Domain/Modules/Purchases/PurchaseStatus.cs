namespace ERP.Domain.Modules.Purchases;

/// <summary>Alış sifarişinin statusu. Qəbul edildikdə məhsul stoku artır.</summary>
public enum PurchaseStatus
{
    Qaralama,       // Draft — sətirlər dəyişdirilə bilər
    Təsdiqlənmiş,   // Təchizatçıya sifariş verilib, qəbul gözlənilir
    QəbulEdilmiş,   // Mal anbara daxil olub — stok artırıldı
    Ləğv            // Ləğv edilmiş
}

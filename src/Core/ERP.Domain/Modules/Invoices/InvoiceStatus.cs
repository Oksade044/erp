namespace ERP.Domain.Modules.Invoices;

/// <summary>Fakturanın ödəniş statusu (ödənilmiş məbləğə görə hesablanır).</summary>
public enum InvoiceStatus
{
    Ödənilməmiş = 1,
    QismənÖdənilmiş = 2,
    Ödənilmiş = 3
}

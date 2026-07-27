namespace ERP.Domain.Modules.Suppliers;

/// <summary>
/// Təchizatçı defter qeydinin növü (#15). Borc — təchizatçıya borc yarandı (məs. alış);
/// Ödəniş — təchizatçıya ödəniş edildi; Danışıq — kommunikasiya/danışıq qeydi;
/// Sənəd — sənəd/fayl əlavəsi. Borc − Ödəniş = qalıq borc.
/// </summary>
public enum SupplierEntryType
{
    Borc = 1,
    Ödəniş = 2,
    Danışıq = 3,
    Sənəd = 4
}

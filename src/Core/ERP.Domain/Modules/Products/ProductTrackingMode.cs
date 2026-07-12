namespace ERP.Domain.Modules.Products;

/// <summary>
/// Məhsulun anbar izləmə rejimi.
/// • Toplu — say ilə izlənir (məs. stullar: 200 ədəd, ayrı-ayrı fərqləndirilmir).
/// • Nüsxə — hər nüsxə fərdi izlənir (barkod/seriya ilə, məs. bahalı avadanlıq).
/// </summary>
public enum ProductTrackingMode
{
    Toplu = 1,
    Nüsxə = 2
}

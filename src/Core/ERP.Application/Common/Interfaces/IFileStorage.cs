namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Fayl saxlama abstraksiyası (TDD §23). Lokalda disk qovluğu, serverdə S3/MinIO — keçid
/// konfiqurasiya ilə, kod dəyişmir. Fayllar heç vaxt DB-də saxlanmır (yalnız yol/metadata).
/// </summary>
public interface IFileStorage
{
    /// <summary>Faylı saxlayır və unikal saxlama açarını (nisbi yol) qaytarır.</summary>
    Task<string> SaveAsync(Stream content, string folder, string fileExtension, CancellationToken ct = default);

    /// <summary>Saxlama açarına görə faylı oxumaq üçün stream açır (yoxdursa null).</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default);

    /// <summary>Faylı silir (varsa).</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);
}

namespace ERP.Application.Common.Interfaces;

/// <summary>
/// QR/barkod generasiyası (TDD §27). İnterfeys Application-da, implementasiya Infrastructure-da.
/// Pure-managed PNG (native asılılıq yoxdur — Linux serverdə də işləyir).
/// </summary>
public interface IBarcodeService
{
    /// <summary>Verilmiş məzmunu QR kod PNG-si kimi qaytarır.</summary>
    byte[] GenerateQrPng(string content, int pixelsPerModule = 10);
}

using ERP.Application.Common.Interfaces;
using QRCoder;

namespace ERP.Infrastructure.Barcodes;

/// <summary>
/// QRCoder ilə QR kod generasiyası (TDD §27). PngByteQRCode pure-managed-dir —
/// System.Drawing və ya native asılılıq yoxdur, Linux serverdə də işləyir.
/// Hər məhsul/nüsxə üçün QR yaradılıb çap oluna bilər; skanlama əl skaneri ilə.
/// </summary>
public sealed class BarcodeService : IBarcodeService
{
    public byte[] GenerateQrPng(string content, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }
}

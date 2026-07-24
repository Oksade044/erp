using ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ERP.Infrastructure.Files;

/// <summary>
/// IFileStorage-in lokal disk implementasiyası (TDD §23). Kök qovluq konfiqurasiyadan
/// (Storage:Root, default "storage"). Saxlama açarı "{folder}/{guid}{ext}" formatındadır —
/// bütün istifadəçilər eyni API vasitəsilə bu faylları oxuyur (lokal fayl, hamıya görünür).
/// Serverə keçəndə bu sinif MinIO/S3 implementasiyası ilə əvəzlənir, qalan kod dəyişmir.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration configuration)
    {
        _root = configuration["Storage:Root"] ?? "storage";
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string folder, string fileExtension, CancellationToken ct = default)
    {
        var safeFolder = folder.Trim('/', '\\');
        var ext = fileExtension.StartsWith('.') ? fileExtension : "." + fileExtension;
        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var key = $"{safeFolder}/{fileName}";

        var folderPath = Path.Combine(_root, safeFolder);
        Directory.CreateDirectory(folderPath);

        var fullPath = Path.Combine(_root, safeFolder, fileName);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs, ct);

        return key;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(key);
        if (fullPath is null || !File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(key);
        if (fullPath is not null && File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    /// <summary>Açarı kök qovluğun içində təhlükəsiz mütləq yola çevirir (path traversal qorunması).</summary>
    private string? ResolvePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var rootFull = Path.GetFullPath(_root);
        var combined = Path.GetFullPath(Path.Combine(_root, key.Replace('\\', '/')));

        // Kök qovluqdan kənara çıxışın qarşısı.
        return combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) ? combined : null;
    }
}

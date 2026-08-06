namespace ERP.Shared.Contracts.App;

/// <summary>
/// Auto-update üçün son versiya məlumatı (serverdə statik version.json).
/// Version — "1.0.0" formatında; Url — quraşdırıcının endirmə linki.
/// </summary>
public sealed record AppVersionInfo(
    string Version,
    string Url,
    string? Notes = null);

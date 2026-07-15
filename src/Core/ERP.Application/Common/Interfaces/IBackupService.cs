namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Verilənlər bazasının backup-ı (TDD §29). Lokalda SQLite online backup;
/// serverdə pg_dump (gələcək). Nəticə: yaradılan backup faylının yolu.
/// </summary>
public interface IBackupService
{
    Task<string> BackupAsync(CancellationToken ct = default);
}

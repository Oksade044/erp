using ERP.Application.Common.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Backup;

/// <summary>
/// SQLite online backup (TDD §29). SqliteConnection.BackupDatabase WAL-ı düzgün idarə edir
/// (baza işləyərkən təhlükəsiz kopya). Backup faylları backups/ qovluğunda tarixlə saxlanılır.
/// Serverdə (PostgreSQL) bu servis pg_dump ilə əvəz olunacaq.
/// </summary>
public sealed class SqliteBackupService(IConfiguration configuration, ILogger<SqliteBackupService> logger)
    : IBackupService
{
    public Task<string> BackupAsync(CancellationToken ct = default)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=erp.db";

        var backupDir = Path.Combine(AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(backupDir);
        var backupPath = Path.Combine(backupDir, $"erp-{DateTime.Now:yyyyMMdd-HHmmss}.db");

        using var source = new SqliteConnection(connectionString);
        using var destination = new SqliteConnection($"Data Source={backupPath}");
        source.Open();
        destination.Open();
        source.BackupDatabase(destination);

        logger.LogInformation("Verilənlər bazası backup-ı yaradıldı: {Path}", backupPath);

        // Köhnə backup-ları təmizlə (son 14 saxla).
        CleanupOldBackups(backupDir, keep: 14);

        return Task.FromResult(backupPath);
    }

    private static void CleanupOldBackups(string dir, int keep)
    {
        var files = Directory.GetFiles(dir, "erp-*.db")
            .OrderByDescending(f => f)
            .Skip(keep);
        foreach (var f in files)
        {
            try { File.Delete(f); } catch { /* ignore */ }
        }
    }
}

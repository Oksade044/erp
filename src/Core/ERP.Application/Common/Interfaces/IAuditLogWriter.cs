namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Biznes hadisələrini audit jurnalına açıq yazmaq üçün (#32 — qiymət dəyişikliyi və s.).
/// Yazılan qeyd cari UnitOfWork.SaveChanges ilə saxlanılır.
/// </summary>
public interface IAuditLogWriter
{
    void Add(string userName, string action, string entityType, string entityId, string? summary);
}

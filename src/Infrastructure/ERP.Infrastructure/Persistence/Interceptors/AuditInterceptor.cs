using ERP.Application.Common.Interfaces;
using ERP.Domain.Common;
using ERP.Domain.Modules.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERP.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges interceptor — audit sahələrini avtomatik doldurur və hard delete-i
/// soft delete-ə çevirir (TDD §13, §20). Heç bir handler əl ilə audit yazmır.
/// </summary>
public sealed class AuditInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        var now = DateTimeOffset.UtcNow;
        var user = currentUser.FullName ?? currentUser.UserName ?? currentUser.UserId ?? "system";

        var logs = new List<AuditLog>();

        foreach (EntityEntry<BaseEntity> entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    logs.Add(BuildLog(entry, now, user, "Yaradıldı"));
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = user;
                    logs.Add(BuildLog(entry, now, user, "Dəyişdirildi"));
                    break;

                case EntityState.Deleted:
                    // Hard delete → soft delete (TDD §13).
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = user;
                    logs.Add(BuildLog(entry, now, user, "Silindi"));
                    break;
            }
        }

        // Audit qeydlərini əlavə et (BaseEntity deyil → yenidən audit olunmur, rekursiya yoxdur).
        if (logs.Count > 0)
            context.Set<AuditLog>().AddRange(logs);
    }

    private static AuditLog BuildLog(EntityEntry<BaseEntity> entry, DateTimeOffset now, string user, string action)
    {
        // Soft-delete-i dəyişiklik kimi yazmayaq — silmə üçün ayrıca summary.
        string? summary = null;
        if (action == "Dəyişdirildi")
        {
            var changed = entry.Properties
                .Where(p => p.IsModified && p.Metadata.Name is not ("UpdatedAt" or "UpdatedBy"
                    or "IsDeleted" or "DeletedAt" or "DeletedBy"))
                .Select(p => p.Metadata.Name)
                .ToList();
            summary = changed.Count > 0 ? "Dəyişən sahələr: " + string.Join(", ", changed) : null;
        }

        return AuditLog.Create(now, user, action, entry.Entity.GetType().Name,
            entry.Entity.Id.ToString(), summary);
    }
}

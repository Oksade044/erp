using ERP.Application.Common.Interfaces;
using ERP.Domain.Modules.Settings;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>Sahə-görünürlük qaydalarına xas repository implementasiyası (TDD §14).</summary>
public sealed class FieldPermissionRepository(AppDbContext context)
    : Repository<FieldPermission>(context), IFieldPermissionRepository
{
    public async Task<FieldPermission?> GetByKeyAsync(string fieldKey, CancellationToken ct = default) =>
        await Set.FirstOrDefaultAsync(f => f.FieldKey == fieldKey, ct);
}

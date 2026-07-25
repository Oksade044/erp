using ERP.Domain.Modules.Settings;

namespace ERP.Application.Common.Interfaces;

/// <summary>Sahə-görünürlük qaydalarına xas repository (TDD §14).</summary>
public interface IFieldPermissionRepository : IRepository<FieldPermission>
{
    /// <summary>Verilmiş sahə açarı üçün qayda (yoxdursa null).</summary>
    Task<FieldPermission?> GetByKeyAsync(string fieldKey, CancellationToken ct = default);
}

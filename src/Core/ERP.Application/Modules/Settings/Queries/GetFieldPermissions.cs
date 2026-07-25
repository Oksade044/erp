using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Domain.Modules.Settings;
using ERP.Shared.Contracts.Settings;

namespace ERP.Application.Modules.Settings.Queries;

/// <summary>Bütün həssas sahələrin görünürlük qaydalarını qaytarır (kataloq + saxlanmış rollar).</summary>
public sealed record GetFieldPermissionsQuery : IRequest<IReadOnlyList<FieldPermissionDto>>;

public sealed class GetFieldPermissionsHandler(IFieldPermissionRepository repo)
    : IRequestHandler<GetFieldPermissionsQuery, IReadOnlyList<FieldPermissionDto>>
{
    // Default: yalnız Admin/Menecer görür (qayda hələ saxlanmayıbsa).
    private static readonly string[] Defaults = ["Admin", "Menecer"];

    public async Task<IReadOnlyList<FieldPermissionDto>> Handle(GetFieldPermissionsQuery request, CancellationToken ct)
    {
        var stored = await repo.ListAsync(ct);
        var byKey = stored.ToDictionary(x => x.FieldKey, x => x);

        return FieldKeys.All.Select(key =>
        {
            var roles = byKey.TryGetValue(key, out var fp)
                ? fp.AllowedRoles.Select(r => r.ToString()).ToList()
                : Defaults.ToList();
            return new FieldPermissionDto(key, FieldKeys.DisplayName(key), roles);
        }).ToList();
    }
}

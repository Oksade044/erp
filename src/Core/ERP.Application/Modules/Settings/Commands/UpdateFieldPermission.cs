using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Settings;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Settings;

namespace ERP.Application.Modules.Settings.Commands;

/// <summary>Bir həssas sahəni görə bilən rolları yeniləyir (yalnız Admin/Menecer — users.manage).</summary>
public sealed record UpdateFieldPermissionCommand(UpdateFieldPermissionRequest Request) : IRequest<Result>;

public sealed class UpdateFieldPermissionHandler(
    IFieldPermissionRepository repo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateFieldPermissionCommand, Result>
{
    public async Task<Result> Handle(UpdateFieldPermissionCommand request, CancellationToken ct)
    {
        var dto = request.Request;

        if (!FieldKeys.All.Contains(dto.FieldKey))
            return Result.Failure($"Naməlum sahə: {dto.FieldKey}");

        // Rol adlarını enum-a çevir (naməlumları at). Admin həmişə daxildir (məhdudlaşdırıla bilməz).
        var roles = new HashSet<Role> { Role.Admin };
        foreach (var name in dto.AllowedRoles)
            if (Enum.TryParse<Role>(name, out var role))
                roles.Add(role);

        var existing = await repo.GetByKeyAsync(dto.FieldKey, ct);
        if (existing is null)
        {
            var created = FieldPermission.Create(dto.FieldKey, roles);
            await repo.AddAsync(created, ct);
        }
        else
        {
            existing.SetRoles(roles);
            repo.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Users;

namespace ERP.Application.Modules.Users;

// ---- Sorğular ----

/// <summary>Bütün rollar (#16).</summary>
public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public sealed class GetRolesHandler(IRoleRepository roles)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken ct)
    {
        var list = await roles.ListOrderedAsync(ct);
        return list.Select(r => new RoleDto(r.Id, r.Name, r.IsSystem, r.Permissions.ToList())).ToList();
    }
}

/// <summary>İcazə kataloqu (matris UI üçün) — açar + aydın ad.</summary>
public sealed record GetPermissionCatalogQuery : IRequest<IReadOnlyList<PermissionInfoDto>>;

public sealed class GetPermissionCatalogHandler
    : IRequestHandler<GetPermissionCatalogQuery, IReadOnlyList<PermissionInfoDto>>
{
    public Task<IReadOnlyList<PermissionInfoDto>> Handle(GetPermissionCatalogQuery request, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PermissionInfoDto>>(
            Permissions.Catalog.Select(c => new PermissionInfoDto(c.Key, c.Label)).ToList());
}

// ---- Əmrlər ----

/// <summary>Yeni rol yaradır (#16).</summary>
public sealed record CreateRoleCommand(CreateRoleRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateRoleHandler(IRoleRepository roles, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        var name = request.Request.Name?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Guid>("Rol adı tələb olunur.");
        if (await roles.GetByNameAsync(name, ct) is not null)
            return Result.Failure<Guid>($"Bu adda rol artıq var: {name}");

        var valid = ValidPermissions(request.Request.Permissions);
        var role = AppRole.Create(name, valid, isSystem: false);
        await roles.AddAsync(role, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(role.Id);
    }

    internal static IEnumerable<string> ValidPermissions(IEnumerable<string>? permissions)
    {
        var known = Permissions.Catalog.Select(c => c.Key).ToHashSet();
        return (permissions ?? []).Where(known.Contains);
    }
}

/// <summary>Rolun icazələrini yeniləyir (#16).</summary>
public sealed record UpdateRolePermissionsCommand(Guid Id, UpdateRolePermissionsRequest Request) : IRequest<Result>;

public sealed class UpdateRolePermissionsHandler(IRoleRepository roles, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateRolePermissionsCommand, Result>
{
    public async Task<Result> Handle(UpdateRolePermissionsCommand request, CancellationToken ct)
    {
        var role = await roles.GetByIdAsync(request.Id, ct);
        if (role is null)
            return Result.Failure($"Rol tapılmadı: {request.Id}");

        // Təhlükəsizlik: Admin rolu heç vaxt icazələrini itirməsin (özünü kilidləməsin).
        if (role.Name == "Admin")
            role.SetPermissions(Permissions.Catalog.Select(c => c.Key));
        else
            role.SetPermissions(CreateRoleHandler.ValidPermissions(request.Request.Permissions));
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

/// <summary>Rolu silir — daxili (sistem) rol silinə bilməz (#16).</summary>
public sealed record DeleteRoleCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteRoleHandler(IRoleRepository roles, IUserRepository users, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteRoleCommand, Result>
{
    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken ct)
    {
        var role = await roles.GetByIdAsync(request.Id, ct);
        if (role is null)
            return Result.Failure($"Rol tapılmadı: {request.Id}");
        if (role.IsSystem)
            return Result.Failure("Daxili rol silinə bilməz.");

        roles.Remove(role);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

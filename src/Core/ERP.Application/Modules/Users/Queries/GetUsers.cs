using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Shared.Contracts.Users;

namespace ERP.Application.Modules.Users.Queries;

/// <summary>Bütün istifadəçiləri qaytarır (parolsuz). TDD §17.</summary>
public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserDto>>;

public sealed class GetUsersHandler(IUserRepository users)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var list = await users.ListAsync(ct);
        return list.Select(u => u.ToDto()).OrderBy(u => u.Username).ToList();
    }
}

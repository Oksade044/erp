using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;

namespace ERP.Application.Modules.Users.Commands;

/// <summary>İstifadəçini silir (soft delete). Sistem admin istifadəçisi qorunur.</summary>
public sealed record DeleteUserCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteUserHandler(IUserRepository users, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteUserCommand, Result>
{
    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.Id, ct);
        if (user is null) return Result.Failure("İstifadəçi tapılmadı.");
        if (string.Equals(user.Username, "admin", System.StringComparison.OrdinalIgnoreCase))
            return Result.Failure("Sistem admin istifadəçisi silinə bilməz.");

        users.Remove(user);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

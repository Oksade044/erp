using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Domain.Modules.Users;
using ERP.Shared.Contracts.Users;
using FluentValidation;

namespace ERP.Application.Modules.Users.Commands;

/// <summary>Yeni istifadəçi yaradır (yalnız users.manage icazəsi ilə). Parol PBKDF2 hash-lənir.</summary>
public sealed record CreateUserCommand(CreateUserRequest Request) : IRequest<Result<Guid>>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Request.Username).NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Role).NotEmpty();
    }
}

public sealed class CreateUserHandler(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher hasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var dto = request.Request;
        var username = dto.Username.Trim().ToLowerInvariant();

        if (await users.GetByUsernameAsync(username, ct) is not null)
            return Result.Failure<Guid>($"Bu istifadəçi adı artıq mövcuddur: {username}");

        // Rol mövcud olmalıdır (dinamik rollar — #16).
        var role = await roles.GetByNameAsync(dto.Role, ct);
        if (role is null)
            return Result.Failure<Guid>($"Belə rol yoxdur: {dto.Role}");

        var (hash, salt) = hasher.Hash(dto.Password);
        var user = User.Create(username, hash, salt, dto.FullName, role.Name);

        await users.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(user.Id);
    }
}

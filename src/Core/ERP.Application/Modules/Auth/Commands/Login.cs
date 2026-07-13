using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Auth;
using FluentValidation;

namespace ERP.Application.Modules.Auth.Commands;

/// <summary>İstifadəçi girişi — istifadəçi adı + parol → JWT tokenlər (TDD §6).</summary>
public sealed record LoginCommand(LoginRequest Request) : IRequest<Result<AuthResponse>>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Request.Username).NotEmpty();
        RuleFor(x => x.Request.Password).NotEmpty();
    }
}

public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    ITokenService tokens,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var dto = request.Request;
        var user = await users.GetByUsernameAsync(dto.Username.Trim().ToLowerInvariant(), ct);

        // Təhlükəsizlik: mövcud olmayan istifadəçi ilə yanlış parol eyni mesaj verir (enumeration qorunması).
        if (user is null || !user.IsActive || !hasher.Verify(dto.Password, user.PasswordHash, user.PasswordSalt))
            return Result.Failure<AuthResponse>("İstifadəçi adı və ya parol yanlışdır.");

        var (accessToken, refreshToken, expiresAt) = tokens.GenerateTokens(user);
        user.SetRefreshToken(refreshToken, DateTimeOffset.UtcNow.AddDays(7));
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new AuthResponse(
            accessToken, refreshToken, expiresAt,
            user.Username, user.FullName, user.Role.ToString(),
            user.GetPermissions().ToList()));
    }
}

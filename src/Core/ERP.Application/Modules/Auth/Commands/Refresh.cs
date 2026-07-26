using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Messaging;
using ERP.Application.Common.Models;
using ERP.Shared.Contracts.Auth;
using FluentValidation;

namespace ERP.Application.Modules.Auth.Commands;

/// <summary>Refresh token ilə yeni access token alır (TDD §6).</summary>
public sealed record RefreshCommand(RefreshRequest Request) : IRequest<Result<AuthResponse>>;

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator() => RuleFor(x => x.Request.RefreshToken).NotEmpty();
}

public sealed class RefreshHandler(
    IUserRepository users,
    IRoleRepository roles,
    ITokenService tokens,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefreshCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshCommand request, CancellationToken ct)
    {
        var user = await users.GetByRefreshTokenAsync(request.Request.RefreshToken, ct);
        if (user is null || !user.IsActive || !user.IsRefreshTokenValid(request.Request.RefreshToken))
            return Result.Failure<AuthResponse>("Refresh token etibarsızdır və ya vaxtı bitib.");

        var permissions = await AuthPermissions.ResolveAsync(roles, user.RoleName, ct);

        var (accessToken, refreshToken, expiresAt) = tokens.GenerateTokens(user, permissions);
        user.SetRefreshToken(refreshToken, DateTimeOffset.UtcNow.AddDays(7));
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new AuthResponse(
            accessToken, refreshToken, expiresAt,
            user.Username, user.FullName, user.RoleName, permissions));
    }
}

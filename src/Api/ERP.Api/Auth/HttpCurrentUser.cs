using System.Security.Claims;
using ERP.Application.Common.Interfaces;

namespace ERP.Api.Auth;

/// <summary>
/// ICurrentUser-in HTTP implementasiyası (TDD §7, §20). Cari istifadəçini HttpContext-dən oxuyur.
/// Auth tam qurulanadək anonim/system kimi işləyir.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? UserName => User?.Identity?.Name;
    public string? FullName => User?.FindFirstValue("fullName");
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permission) =>
        User?.HasClaim("permission", permission) ?? false;
}

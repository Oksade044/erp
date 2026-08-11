using ERP.Shared.Contracts.Auth;
using Microsoft.JSInterop;

namespace ERP.Web.Services;

/// <summary>
/// Sessiya vəziyyəti — JWT token + cari istifadəçi. Token localStorage-də saxlanılır ki,
/// səhifə yenilənəndə/yenidən açılanda istifadəçi yenidən daxil olmasın.
/// </summary>
public sealed class AppState(IJSRuntime js)
{
    private const string TokenKey = "erp_token";
    private const string NameKey = "erp_fullname";
    private const string RoleKey = "erp_role";

    public string? AccessToken { get; private set; }
    public string? FullName { get; private set; }
    public string? Role { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);

    public event Action? Changed;

    /// <summary>Səhifə açılışında localStorage-dən sessiyanı yükləyir.</summary>
    public async Task InitAsync()
    {
        AccessToken = await GetItem(TokenKey);
        FullName = await GetItem(NameKey);
        Role = await GetItem(RoleKey);
        Changed?.Invoke();
    }

    public async Task SetAuthAsync(AuthResponse auth)
    {
        AccessToken = auth.AccessToken;
        FullName = auth.FullName;
        Role = auth.Role;
        await SetItem(TokenKey, AccessToken);
        await SetItem(NameKey, FullName);
        await SetItem(RoleKey, Role);
        Changed?.Invoke();
    }

    public async Task LogoutAsync()
    {
        AccessToken = FullName = Role = null;
        await Remove(TokenKey); await Remove(NameKey); await Remove(RoleKey);
        Changed?.Invoke();
    }

    private async Task<string?> GetItem(string key)
    {
        try { return await js.InvokeAsync<string?>("localStorage.getItem", key); }
        catch { return null; }
    }
    private async Task SetItem(string key, string? value)
    {
        try { await js.InvokeVoidAsync("localStorage.setItem", key, value ?? ""); } catch { }
    }
    private async Task Remove(string key)
    {
        try { await js.InvokeVoidAsync("localStorage.removeItem", key); } catch { }
    }
}

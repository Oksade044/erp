using ERP.Shared.Contracts.Auth;

namespace ERP.Mobile.Services;

/// <summary>
/// Sessiya vəziyyəti — token, cari istifadəçi və server ünvanı (VPS-ə keçəndə dəyişdirilə bilər).
/// Sadə singleton; DI ilə paylaşılır.
/// </summary>
public sealed class AppState
{
    /// <summary>
    /// API baza ünvanı. Android emulyatorunda host "localhost" = 10.0.2.2.
    /// Real cihaz/VPS üçün Preferences-də saxlanılan ünvan istifadə olunur.
    /// </summary>
    public string BaseUrl
    {
        // Default: VPS serveri (HTTPS). Giriş ekranında dəyişilə bilər.
        // Emulyator/lokal üçün http://10.0.2.2:5080 və ya LAN IP yazmaq olar.
        get => Preferences.Get("api_base_url", "https://186.240.145.239.sslip.io");
        set => Preferences.Set("api_base_url", value);
    }

    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public AuthResponse? User { get; set; }

    public bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        User = null;
    }
}

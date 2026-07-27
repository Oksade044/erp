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
        // Default: bu kompüterin LAN IP-si (eyni Wi-Fi-dakı telefon üçün). Giriş ekranında dəyişilə bilər.
        // Emulyator üçün http://10.0.2.2:5080; VPS üçün https://domeniniz.
        get => Preferences.Get("api_base_url", "http://192.168.100.50:5080");
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

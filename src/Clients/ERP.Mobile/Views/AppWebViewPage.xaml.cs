namespace ERP.Mobile.Views;

/// <summary>
/// Tam-ekran WebView qabığı — web tətbiqini (PWA) native tətbiq kimi göstərir.
/// Mətn seçmə/kopyalama/zoom web tərəfdə (CSS) və platforma handler-ində söndürülüb.
/// Yüklənmə uğursuz olsa boş ağ ekran əvəzinə xəta ekranı + "yenidən cəhd" göstərilir.
/// </summary>
public partial class AppWebViewPage : ContentPage
{
    public AppWebViewPage()
    {
        InitializeComponent();
    }

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result == WebNavigationResult.Success)
        {
            // Sayt yükləndi — splash və xəta ekranını gizlət.
            Splash.IsVisible = false;
            ErrorView.IsVisible = false;
        }
        else
        {
            // Yüklənmə alınmadı (server əlçatmaz / internet yox) — boş ağ ekran ƏVƏZİNƏ xəta göstər.
            Splash.IsVisible = false;
            ErrorView.IsVisible = true;
        }
    }

    private void OnRetry(object? sender, EventArgs e)
    {
        ErrorView.IsVisible = false;
        Splash.IsVisible = true;
        Web.Reload();
    }
}

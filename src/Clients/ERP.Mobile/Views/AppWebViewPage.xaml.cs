namespace ERP.Mobile.Views;

/// <summary>
/// Tam-ekran WebView qabığı — web tətbiqini (PWA) native tətbiq kimi göstərir.
/// Mətn seçmə/kopyalama/zoom web tərəfdə (CSS) və platforma handler-ində söndürülüb.
/// </summary>
public partial class AppWebViewPage : ContentPage
{
    public AppWebViewPage()
    {
        InitializeComponent();
    }

    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        // Yalnız öz saytımızda qalırıq (xarici linklər webview-i tərk etməsin).
    }

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        // Səhifə yükləndi — splash-i gizlət.
        Splash.IsVisible = false;
    }
}

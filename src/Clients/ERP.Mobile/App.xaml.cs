namespace ERP.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Tətbiq web tətbiqini (PWA) tam-ekran WebView-də göstərir — istifadəçi bunun web
        // olduğunu hiss etmir (seçmə/kopyalama/zoom söndürülüb). Giriş də web tərəfdədir.
        return new Window(new Views.AppWebViewPage());
    }
}

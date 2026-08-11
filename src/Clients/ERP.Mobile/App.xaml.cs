using ERP.Mobile.Views;

namespace ERP.Mobile;

public partial class App : Application
{
	public static IServiceProvider Services { get; private set; } = null!;

	public App(IServiceProvider services)
	{
		InitializeComponent();
		Services = services;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Tətbiq artıq web tətbiqini (PWA) tam-ekran WebView-də göstərir — istifadəçi
		// bunun web olduğunu hiss etmir (seçmə/kopyalama/zoom söndürülüb). Giriş də web tərəfdədir.
		return new Window(new Views.AppWebViewPage());
	}

	// Köhnə native naviqasiya artıq istifadə olunmur (webview qabığına keçilib) — stub qalır.
	public static void GoToMain() { }
	public static void GoToLogin() { }
}

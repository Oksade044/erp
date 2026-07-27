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
		// Giriş ekranından başla; uğurlu login-dən sonra AppShell-ə keçilir.
		var login = Services.GetService<LoginPage>()!;
		return new Window(new NavigationPage(login));
	}

	/// <summary>Uğurlu login-dən sonra əsas naviqasiyaya (tab-lar) keç.</summary>
	public static void GoToMain()
	{
		if (Current?.Windows.Count > 0)
			Current.Windows[0].Page = Services.GetService<AppShell>()!;
	}

	/// <summary>Çıxış — sessiyanı təmizlə və giriş ekranına qayıt.</summary>
	public static void GoToLogin()
	{
		if (Current?.Windows.Count > 0)
			Current.Windows[0].Page = new NavigationPage(Services.GetService<LoginPage>()!);
	}
}

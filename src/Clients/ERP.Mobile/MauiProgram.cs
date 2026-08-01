using ERP.Mobile.Services;
using ERP.Mobile.ViewModels;
using ERP.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace ERP.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Xidmətlər
		builder.Services.AddSingleton<AppState>();
		builder.Services.AddSingleton<MobileApiClient>();

		// ViewModel-lər
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<MyOrdersViewModel>();
		builder.Services.AddTransient<OrderDetailViewModel>();
		builder.Services.AddTransient<NewOrderViewModel>();
		builder.Services.AddTransient<FinanceViewModel>();
		builder.Services.AddTransient<DebtViewModel>();
		builder.Services.AddTransient<ProfileViewModel>();

		// Naviqasiya qabığı
		builder.Services.AddTransient<AppShell>();

		// Səhifələr
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<MyOrdersPage>();
		builder.Services.AddTransient<OrderDetailPage>();
		builder.Services.AddTransient<NewOrderPage>();
		builder.Services.AddTransient<FinancePage>();
		builder.Services.AddTransient<DebtPage>();
		builder.Services.AddTransient<ProfilePage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

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

		// WebView-i native tərəfdə "tətbiq kimi" göstər — zoom yox, uzun-basma/seçmə/menyu yox.
		Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("ErpNativeFeel", (handler, view) =>
		{
#if ANDROID
			var wv = handler.PlatformView;
			wv.Settings.BuiltInZoomControls = false;
			wv.Settings.DisplayZoomControls = false;
			wv.Settings.SetSupportZoom(false);
			wv.Settings.TextZoom = 100;
			wv.Settings.DomStorageEnabled = true;           // localStorage (token) işləsin
			wv.LongClickable = false;
			wv.HapticFeedbackEnabled = false;
			wv.SetOnLongClickListener(new NoLongClick());   // uzun-basma menyusu (seçmə/kopyala) söndür
			wv.OverScrollMode = Android.Views.OverScrollMode.Never;
#elif IOS || MACCATALYST
			var wv = handler.PlatformView;
			wv.ScrollView.Bounces = false;
			wv.AllowsLinkPreview = false;
			wv.ScrollView.MaximumZoomScale = 1;
			wv.ScrollView.MinimumZoomScale = 1;
#endif
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ERP.Desktop.Services;
using ERP.Desktop.ViewModels;
using ERP.Desktop.Views;
using ERP.Shared.Contracts.Auth;

namespace ERP.Desktop;

public partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private ErpApiClient _api = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Son çarə: tutulmayan exception-ları fayla yaz (proqram sükutla ölməsin, iz qalsın).
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogCrash("AppDomain", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogCrash("UnobservedTask", args.Exception);
            args.SetObserved();
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;

            // Tək HttpClient bütün ekranlar arasında bölüşülür (JWT token da burada saxlanılır).
            var http = new HttpClient { BaseAddress = new Uri("http://localhost:5080") };
            _api = new ErpApiClient(http);

            ShowLogin();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowLogin()
    {
        var login = new LoginWindow
        {
            DataContext = new LoginViewModel(_api, OnLoggedIn)
        };
        SwitchMainWindow(login);
    }

    private void OnLoggedIn(AuthResponse auth)
    {
        var main = new MainWindow
        {
            DataContext = new MainViewModel(_api, auth, onLogout: () =>
            {
                _api.SetBearerToken(null);
                ShowLogin();
            })
        };
        SwitchMainWindow(main);
    }

    /// <summary>Cari əsas pəncərəni yenisi ilə əvəz edir (login ↔ main keçidi).</summary>
    private void SwitchMainWindow(Avalonia.Controls.Window window)
    {
        var old = _desktop!.MainWindow;
        _desktop.MainWindow = window;
        window.Show();
        old?.Close();
    }

    private static void LogCrash(string source, Exception? ex)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}: {ex}\n";
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "desktop-errors.log"), line);
        }
        catch { /* loglama özü də uğursuz olsa, susduraq */ }
    }
}

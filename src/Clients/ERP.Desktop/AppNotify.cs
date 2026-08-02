using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ERP.Desktop;

/// <summary>
/// Ekran ortası bildiriş host-u (#4). MainWindow-da overlay bura bağlanır; VM-lər
/// <see cref="AppNotify.Show"/> ilə mesaj göndərir — müştəri əlavə edildi, sifariş təsdiqləndi və s.
/// </summary>
public sealed partial class NotificationHost : ObservableObject
{
    [ObservableProperty] private string? _message;
    [ObservableProperty] private bool _isVisible;
    private CancellationTokenSource? _cts;

    public async void Show(string message, int ms = 2600)
    {
        Message = message;
        IsVisible = true;
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        try { await Task.Delay(ms, token); if (!token.IsCancellationRequested) IsVisible = false; }
        catch (TaskCanceledException) { /* növbəti bildiriş gəldi */ }
    }
}

/// <summary>Qlobal bildiriş — istənilən VM-dən çağırıla bilər (statik host).</summary>
public static class AppNotify
{
    public static NotificationHost Host { get; } = new();

    public static void Show(string message)
    {
        if (Dispatcher.UIThread.CheckAccess()) Host.Show(message);
        else Dispatcher.UIThread.Post(() => Host.Show(message));
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ERP.Desktop.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private CancellationTokenSource? _debounceCts;

    /// <summary>
    /// Canlı axtarış (Live Search) üçün ortaq köməkçi: istifadəçi yazmağı ~350 ms
    /// dayandıranda <paramref name="reload"/> çağırılır. Hər yeni hərf köhnə gözləməni ləğv edir.
    /// </summary>
    protected void DebounceReload(Func<Task> reload, int delayMs = 350)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = RunAsync();

        async Task RunAsync()
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (!token.IsCancellationRequested)
                    await reload();
            }
            catch (TaskCanceledException) { /* növbəti hərf gəldi */ }
        }
    }
}

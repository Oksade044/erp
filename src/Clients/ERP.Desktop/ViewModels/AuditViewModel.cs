using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Audit;

namespace ERP.Desktop.ViewModels;

/// <summary>Audit jurnalı ekranı (#26) — kim nə vaxt nə etdi. Yalnız səlahiyyətli istifadəçi.</summary>
public partial class AuditViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<AuditLogDto> Logs { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private string? _status;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);

    [RelayCommand]
    private async Task LoadAsync()
    {
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetAuditLogsAsync(Search);
            Logs.Clear();
            if (result is not null)
                foreach (var l in result.Items) Logs.Add(l);
            Status = $"{Logs.Count} qeyd (ən yeni öndə)";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
    }
}

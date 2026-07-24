using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Warehouses;

namespace ERP.Desktop.ViewModels;

/// <summary>Anbarlar ekranı — siyahı, axtarış və yeni anbar əlavəsi (API üzərindən).</summary>
public partial class WarehousesViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<WarehouseDto> Warehouses { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Yeni anbar forması
    [ObservableProperty] private string? _newName;
    [ObservableProperty] private string? _newCode;
    [ObservableProperty] private string? _newCity;
    [ObservableProperty] private string? _newPhone;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetWarehousesAsync(Search);
            Warehouses.Clear();
            if (result is not null)
                foreach (var w in result.Items) Warehouses.Add(w);
            Status = $"{Warehouses.Count} anbar";
        }
        catch (System.Exception ex)
        {
            Status = $"Xəta: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName) || string.IsNullOrWhiteSpace(NewCode))
        {
            Status = "Ad və kod tələb olunur.";
            return;
        }

        IsBusy = true;
        try
        {
            var (ok, error) = await api.CreateWarehouseAsync(new CreateWarehouseRequest(
                Name: NewName!, Code: NewCode!, Phone: NewPhone, City: NewCity));

            if (ok)
            {
                Status = "Anbar əlavə olundu.";
                NewName = NewCode = NewCity = NewPhone = null;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }
}

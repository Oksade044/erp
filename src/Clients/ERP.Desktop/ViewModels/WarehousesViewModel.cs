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
    [ObservableProperty] private WarehouseDto? _selected;
    private System.Guid? _editId;

    /// <summary>Seçilmiş anbarı silir (soft delete).</summary>
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Silmək üçün anbar seçin."); return; }
        var name = Selected.Name;
        var (ok, err) = await api.DeleteWarehouseAsync(Selected.Id);
        ERP.Desktop.AppNotify.Show(ok ? $"Anbar silindi: {name}" : err ?? "Silinmədi.");
        if (ok) await LoadAsync();
    }

    /// <summary>Seçilmiş anbarı forma sahələrinə yükləyir — 'Yadda saxla' yeniləyir.</summary>
    [RelayCommand]
    private void EditSelected()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Redaktə üçün anbar seçin."); return; }
        _editId = Selected.Id;
        NewName = Selected.Name; NewCode = Selected.Code; NewCity = Selected.City; NewPhone = Selected.Phone;
        ERP.Desktop.AppNotify.Show("Məlumatı dəyişib 'Yadda saxla' basın.");
    }

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);
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
            bool ok; string? error;
            if (_editId is { } id)
                (ok, error) = await api.UpdateWarehouseAsync(id, new UpdateWarehouseRequest(
                    Name: NewName!, Code: NewCode!, Phone: NewPhone, City: NewCity));
            else
                (ok, error) = await api.CreateWarehouseAsync(new CreateWarehouseRequest(
                    Name: NewName!, Code: NewCode!, Phone: NewPhone, City: NewCity));

            if (ok)
            {
                Status = _editId is null ? "Anbar əlavə olundu." : "Anbar yeniləndi.";
                ERP.Desktop.AppNotify.Show(Status);
                _editId = null;
                NewName = NewCode = NewCity = NewPhone = null;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }
}

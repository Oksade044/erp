using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Products;

namespace ERP.Desktop.ViewModels;

/// <summary>Kateqoriyalar (#4) — ayrıca bölmə: siyahı + yeni kateqoriya yaratma.</summary>
public partial class CategoriesViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    [ObservableProperty] private CategoryDto? _selected;
    [ObservableProperty] private string? _newName;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;
    private System.Guid? _editId;

    [RelayCommand]
    private void EditSelected()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Kateqoriya seçin."); return; }
        NewName = Selected.Name; _editId = Selected.Id;
        ERP.Desktop.AppNotify.Show("Adı dəyişib 'Yadda saxla' basın.");
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (Selected is null) { ERP.Desktop.AppNotify.Show("Silmək üçün kateqoriya seçin."); return; }
        var name = Selected.Name;
        var (ok, err) = await api.DeleteCategoryAsync(Selected.Id);
        ERP.Desktop.AppNotify.Show(ok ? $"Kateqoriya silindi: {name}" : err ?? "Silinmədi.");
        if (ok) await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var list = await api.GetCategoriesAsync();
            Categories.Clear();
            if (list is not null) foreach (var c in list) Categories.Add(c);
            Status = $"{Categories.Count} kateqoriya";
        }
        catch (System.Exception ex) { Status = $"Xəta: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) { ERP.Desktop.AppNotify.Show("Kateqoriya adı tələb olunur."); return; }
        bool ok; string? err;
        if (_editId is { } id) (ok, err) = await api.UpdateCategoryAsync(id, NewName!.Trim());
        else (ok, err) = await api.CreateCategoryAsync(NewName!.Trim());
        if (!ok) { ERP.Desktop.AppNotify.Show(err ?? "Alınmadı."); return; }
        ERP.Desktop.AppNotify.Show(_editId is null ? $"Kateqoriya yaradıldı: {NewName}" : $"Kateqoriya yeniləndi: {NewName}");
        _editId = null; NewName = null;
        await LoadAsync();
    }
}

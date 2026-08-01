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

    [ObservableProperty] private string? _newName;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

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
        if (string.IsNullOrWhiteSpace(NewName)) { Status = "Kateqoriya adı tələb olunur."; return; }
        var (ok, err) = await api.CreateCategoryAsync(NewName!.Trim());
        if (!ok) { Status = err ?? "Kateqoriya yaradılmadı."; return; }
        Status = $"Kateqoriya yaradıldı: {NewName}";
        NewName = null;
        await LoadAsync();
    }
}

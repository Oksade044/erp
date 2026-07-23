using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Suppliers;

namespace ERP.Desktop.ViewModels;

/// <summary>Təchizatçılar ekranı — siyahı, axtarış və yeni təchizatçı əlavəsi (API üzərindən).</summary>
public partial class SuppliersViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<SupplierDto> Suppliers { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Yeni təchizatçı forması
    [ObservableProperty] private string? _newName;
    [ObservableProperty] private string? _newPhone;
    [ObservableProperty] private string? _newContactPerson;
    [ObservableProperty] private string? _newEmail;
    [ObservableProperty] private string? _newCity;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetSuppliersAsync(Search);
            Suppliers.Clear();
            if (result is not null)
                foreach (var s in result.Items) Suppliers.Add(s);
            Status = $"{Suppliers.Count} təchizatçı";
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
        if (string.IsNullOrWhiteSpace(NewName) || string.IsNullOrWhiteSpace(NewPhone))
        {
            Status = "Ad və telefon tələb olunur.";
            return;
        }

        IsBusy = true;
        try
        {
            var (ok, error) = await api.CreateSupplierAsync(new CreateSupplierRequest(
                Name: NewName!, Phone: NewPhone!,
                ContactPerson: NewContactPerson, Email: NewEmail, City: NewCity));

            if (ok)
            {
                Status = "Təchizatçı əlavə olundu.";
                NewName = NewPhone = NewContactPerson = NewEmail = NewCity = null;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }
}

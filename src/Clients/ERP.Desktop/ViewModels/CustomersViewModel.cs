using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Customers;

namespace ERP.Desktop.ViewModels;

/// <summary>Müştərilər ekranı — siyahı, axtarış və yeni müştəri əlavəsi (API üzərindən).</summary>
public partial class CustomersViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<CustomerDto> Customers { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Yeni müştəri forması
    [ObservableProperty] private string _newType = "Fərdi";
    [ObservableProperty] private string? _newName;
    [ObservableProperty] private string? _newPhone;
    [ObservableProperty] private string? _newEmail;
    [ObservableProperty] private string? _newCity;

    public string[] CustomerTypes { get; } = ["Fərdi", "Korporativ"];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetCustomersAsync(Search);
            Customers.Clear();
            if (result is not null)
                foreach (var c in result.Items) Customers.Add(c);
            Status = $"{Customers.Count} müştəri";
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
            var (ok, error) = await api.CreateCustomerAsync(new CreateCustomerRequest(
                Type: NewType, Name: NewName!, Phone: NewPhone!,
                Email: NewEmail, City: NewCity));

            if (ok)
            {
                Status = "Müştəri əlavə olundu.";
                NewName = NewPhone = NewEmail = NewCity = null;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }
}

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Customers;

namespace ERP.Desktop.ViewModels;

/// <summary>Müştərilər ekranı — siyahı, axtarış və yeni müştəri əlavəsi (API üzərindən).</summary>
public partial class CustomersViewModel(ErpApiClient api, bool createMode = false) : ViewModelBase
{
    /// <summary>#11 — yalnız müştəri yaratma ekranı (siyahı ayrı bölmədədir).</summary>
    public bool CreateMode { get; } = createMode;
    public bool ListMode => !createMode;

    public ObservableCollection<CustomerDto> Customers { get; } = [];

    [ObservableProperty] private string? _search;

    /// <summary>Canlı axtarış — yazıldıqca süzülür (Enter da işləyir).</summary>
    partial void OnSearchChanged(string? value) => DebounceReload(LoadAsync);
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Yeni müştəri forması
    [ObservableProperty] private string _newType = "Fərdi";
    [ObservableProperty] private string? _newName;
    [ObservableProperty] private string? _newPhone;
    [ObservableProperty] private string? _newEmail;
    [ObservableProperty] private string? _newCity;
    [ObservableProperty] private string? _newAddress;
    [ObservableProperty] private string? _newNote;
    // #1 Əlaqələndirmə + Maliyyə
    [ObservableProperty] private string? _newWhatsApp;
    [ObservableProperty] private string? _newRepresentative;
    [ObservableProperty] private decimal _newDebt;
    [ObservableProperty] private string _newDebtCurrency = "AZN";

    public string[] CustomerTypes { get; } = ["Fərdi", "Korporativ"];
    public string[] Currencies { get; } = ["AZN", "USD", "EUR"];
    /// <summary>Təmsilçi seçim siyahısı (#1 — Mərkəz + təmsilçilər).</summary>
    public ObservableCollection<string> RepresentativeNames { get; } = ["Mərkəz"];

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

            // Təmsilçi siyahısını təzələ (#4 — yeni işçi dərhal görünsün).
            try
            {
                var emps = await api.GetEmployeesAsync(null);
                if (emps is not null)
                    foreach (var e in emps.Items) if (!RepresentativeNames.Contains(e.FullName)) RepresentativeNames.Add(e.FullName);
            }
            catch { /* siyahı boş qala bilər */ }
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
                Email: NewEmail, City: NewCity, AddressLine: NewAddress, Notes: NewNote,
                WhatsApp: NewWhatsApp, RepresentativeName: NewRepresentative,
                Debt: NewDebt, DebtCurrency: NewDebtCurrency));

            if (ok)
            {
                Status = "Müştəri əlavə olundu.";
                NewName = NewPhone = NewEmail = NewCity = NewAddress = NewNote = NewWhatsApp = NewRepresentative = null;
                NewDebt = 0;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }
}

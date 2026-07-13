using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Products;

namespace ERP.Desktop.ViewModels;

/// <summary>Məhsullar ekranı — icarə avadanlığı kataloqu, siyahı, axtarış və əlavə.</summary>
public partial class ProductsViewModel(ErpApiClient api) : ViewModelBase
{
    public ObservableCollection<ProductDto> Products { get; } = [];

    [ObservableProperty] private string? _search;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _status;

    // Yeni məhsul forması
    [ObservableProperty] private string? _newSku;
    [ObservableProperty] private string? _newName;
    [ObservableProperty] private decimal _newRentalPrice;
    [ObservableProperty] private string _newTrackingMode = "Toplu";
    [ObservableProperty] private int _newStock;
    [ObservableProperty] private string? _newCategory;

    public string[] TrackingModes { get; } = ["Toplu", "Nüsxə"];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        Status = "Yüklənir...";
        try
        {
            var result = await api.GetProductsAsync(Search);
            Products.Clear();
            if (result is not null)
                foreach (var p in result.Items) Products.Add(p);
            Status = $"{Products.Count} məhsul";
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
        if (string.IsNullOrWhiteSpace(NewSku) || string.IsNullOrWhiteSpace(NewName))
        {
            Status = "SKU və ad tələb olunur.";
            return;
        }

        IsBusy = true;
        try
        {
            var (ok, error) = await api.CreateProductAsync(new CreateProductRequest(
                Sku: NewSku!, Name: NewName!, RentalPrice: NewRentalPrice,
                TrackingMode: NewTrackingMode, StockQuantity: NewStock, Category: NewCategory));

            if (ok)
            {
                Status = "Məhsul əlavə olundu.";
                NewSku = NewName = NewCategory = null;
                NewRentalPrice = 0; NewStock = 0;
                await LoadAsync();
            }
            else Status = error ?? "Əlavə edilmədi.";
        }
        finally { IsBusy = false; }
    }
}

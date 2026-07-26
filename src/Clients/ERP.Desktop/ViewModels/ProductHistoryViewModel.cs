using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Products;

namespace ERP.Desktop.ViewModels;

/// <summary>Məhsul tarixçəsi (#38) — kim, kimə, neçəyə, nə vaxt istifadə edib.</summary>
public partial class ProductHistoryViewModel : ViewModelBase
{
    private readonly ErpApiClient _api;
    private readonly Guid _productId;

    public string Title { get; }
    public ObservableCollection<ProductHistoryRowDto> Rows { get; } = [];
    [ObservableProperty] private string? _status;

    public ProductHistoryViewModel(ErpApiClient api, ProductDto product)
    {
        _api = api;
        _productId = product.Id;
        Title = $"Tarixçə — {product.Name} ({product.Sku})";
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Status = "Yüklənir...";
        try
        {
            var rows = await _api.GetProductHistoryAsync(_productId);
            Rows.Clear();
            if (rows is not null) foreach (var r in rows) Rows.Add(r);
            Status = Rows.Count == 0 ? "Bu məhsul hələ heç bir sifarişdə istifadə olunmayıb." : $"{Rows.Count} qeyd";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
    }
}

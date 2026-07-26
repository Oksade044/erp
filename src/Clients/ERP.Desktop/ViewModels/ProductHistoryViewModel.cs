using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Products;

namespace ERP.Desktop.ViewModels;

/// <summary>Məhsul tarixçəsi (#38) — kim, kimə, neçəyə, nə vaxt istifadə edib. Sətrə klik → sifariş detalı.</summary>
public partial class ProductHistoryViewModel : ViewModelBase
{
    private readonly ErpApiClient _api;
    private readonly Guid _productId;

    public string Title { get; }
    public ObservableCollection<ProductHistoryRowDto> Rows { get; } = [];
    [ObservableProperty] private ProductHistoryRowDto? _selected;
    [ObservableProperty] private string? _status;

    public ProductHistoryViewModel(ErpApiClient api, Guid productId, string title)
    {
        _api = api;
        _productId = productId;
        Title = title;
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
            Status = Rows.Count == 0
                ? "Bu məhsul hələ heç bir sifarişdə istifadə olunmayıb."
                : $"{Rows.Count} qeyd — sətrə iki dəfə klik edib sifariş detalına baxın.";
        }
        catch (Exception ex) { Status = $"Xəta: {ex.Message}"; }
    }

    /// <summary>Seçilmiş tarixçə sətrinin sifarişi üçün detal VM-i (kod-arxasından çağırılır).</summary>
    public async Task<OrderDetailViewModel?> CreateOrderDetailAsync()
    {
        if (Selected is null) return null;
        var result = await _api.GetOrdersAsync(Selected.OrderNumber);
        var order = result?.Items.FirstOrDefault(o => o.OrderNumber == Selected.OrderNumber);
        return order is null ? null : new OrderDetailViewModel(_api, order);
    }
}

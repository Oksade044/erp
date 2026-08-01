using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ERP.Desktop.Services;
using ERP.Shared.Contracts.Products;

namespace ERP.Desktop.ViewModels;

/// <summary>Seçilə bilən məhsul (checkbox).</summary>
public partial class PickProduct(ProductDto product) : ObservableObject
{
    public ProductDto Product { get; } = product;
    public string Name => Product.Name;
    public string Sku => Product.Sku;
    public decimal RentalPrice => Product.RentalPrice;
    [ObservableProperty] private bool _isSelected;
}

/// <summary>
/// Kateqoriya-məhsul seçim pəncərəsi (#5). Sol tərəfdə kateqoriyalar, sağda həmin kateqoriyanın
/// məhsulları; checkbox ilə bir neçəsi seçilir, "Əlavə et" hamısını birdəfəyə sifarişə qaytarır.
/// Kateqoriyalar görünməyə davam edir; sürətli live-search.
/// </summary>
public partial class ProductPickerViewModel(ErpApiClient api) : ViewModelBase
{
    private List<ProductDto> _all = [];

    public ObservableCollection<string> Categories { get; } = [];
    public ObservableCollection<PickProduct> Products { get; } = [];

    [ObservableProperty] private string _selectedCategory = "Hamısı";
    [ObservableProperty] private string? _search;
    [ObservableProperty] private string? _status;

    /// <summary>"Əlavə et" basıldıqda seçilmiş məhsullar (kod-arxası bunu oxuyur).</summary>
    public List<ProductDto> Picked { get; } = [];
    public bool Confirmed { get; private set; }

    partial void OnSelectedCategoryChanged(string value) => Refilter();
    partial void OnSearchChanged(string? value) => DebounceReload(async () => Refilter());

    public async Task InitAsync()
    {
        var cats = await api.GetCategoriesAsync();
        Categories.Clear();
        Categories.Add("Hamısı");
        if (cats is not null) foreach (var c in cats) Categories.Add(c.Name);

        var prods = await api.GetProductsAsync(null);
        _all = prods?.Items?.ToList() ?? [];
        Refilter();
    }

    private void Refilter()
    {
        IEnumerable<ProductDto> q = _all;
        if (SelectedCategory is not null && SelectedCategory != "Hamısı")
            q = q.Where(p => string.Equals(p.Category, SelectedCategory, System.StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim().ToLowerInvariant();
            q = q.Where(p => p.Name.ToLowerInvariant().Contains(s) || p.Sku.ToLowerInvariant().Contains(s));
        }

        // Seçilmişləri qoru (yenidən filtrləyəndə itməsin).
        var selectedIds = Products.Where(p => p.IsSelected).Select(p => p.Product.Id).ToHashSet();
        Products.Clear();
        foreach (var p in q.OrderBy(p => p.Name))
            Products.Add(new PickProduct(p) { IsSelected = selectedIds.Contains(p.Id) });
        Status = $"{Products.Count} məhsul";
    }

    [RelayCommand]
    private void Confirm()
    {
        Picked.Clear();
        Picked.AddRange(_all.Where(p => Products.Any(pp => pp.IsSelected && pp.Product.Id == p.Id)));
        Confirmed = true;
    }
}

using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ERP.Desktop.ViewModels;

namespace ERP.Desktop.Views;

public partial class OrdersView : UserControl
{
    public OrdersView()
    {
        InitializeComponent();
        // Sahə icazəsi (order.creator) — "Yaradan" sütunu yalnız icazə varsa görünür.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not OrdersViewModel vm) return;
            foreach (var col in OrdersGrid.Columns)
                if (col.Header as string == "Yaradan")
                    col.IsVisible = vm.CanViewCreator;
        };
    }

    /// <summary>Seçilmiş sifarişin tam detal kartını açır (#21).</summary>
    private void OnOrderDetail(object? sender, RoutedEventArgs e) => OpenDetail();

    private void OnOrderDoubleTapped(object? sender, RoutedEventArgs e) => OpenDetail();

    /// <summary>Kateqoriya-məhsul seçim pəncərəsini açır (#5); seçilənləri sifariş sətirlərinə əlavə edir.</summary>
    private async void OnOpenPicker(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OrdersViewModel vm) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;

        var picker = vm.CreatePicker();
        var window = new ProductPickerWindow { DataContext = picker };
        await picker.InitAsync();
        var ok = await window.ShowDialog<bool>(owner);
        if (ok)
        {
            // #1 düzəlişi — seçilmişləri birbaşa oxu (Command/Click yarışından qaçmaq üçün).
            var chosen = picker.Products.Where(p => p.IsSelected).Select(p => p.Product).ToList();
            vm.AddPickedProducts(chosen);
        }
    }

    private void OpenDetail()
    {
        if (DataContext is not OrdersViewModel vm) return;
        var detail = vm.CreateDetail();
        if (detail is null) return;

        var window = new OrderDetailWindow { DataContext = detail };

        if (TopLevel.GetTopLevel(this) is Window owner)
            window.Show(owner);
        else
            window.Show();
    }
}

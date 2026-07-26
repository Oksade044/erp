using Avalonia.Controls;
using Avalonia.Interactivity;
using ERP.Desktop.ViewModels;

namespace ERP.Desktop.Views;

public partial class StockView : UserControl
{
    public StockView() => InitializeComponent();

    /// <summary>Stok sətrinə iki dəfə klik → məhsul tarixçəsi.</summary>
    private void OnLevelDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StockViewModel vm) return;
        var detail = vm.CreateHistory();
        if (detail is null) return;
        var window = new ProductHistoryWindow { DataContext = detail };
        if (TopLevel.GetTopLevel(this) is Window owner) window.Show(owner);
        else window.Show();
    }
}

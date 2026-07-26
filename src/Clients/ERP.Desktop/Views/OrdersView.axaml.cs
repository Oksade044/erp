using Avalonia.Controls;
using Avalonia.Interactivity;
using ERP.Desktop.ViewModels;

namespace ERP.Desktop.Views;

public partial class OrdersView : UserControl
{
    public OrdersView() => InitializeComponent();

    /// <summary>Seçilmiş sifarişin tam detal kartını açır (#21).</summary>
    private void OnOrderDetail(object? sender, RoutedEventArgs e) => OpenDetail();

    private void OnOrderDoubleTapped(object? sender, RoutedEventArgs e) => OpenDetail();

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

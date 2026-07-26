using Avalonia.Controls;
using Avalonia.Interactivity;
using ERP.Desktop.ViewModels;

namespace ERP.Desktop.Views;

public partial class ProductHistoryWindow : Window
{
    public ProductHistoryWindow() => InitializeComponent();

    /// <summary>Tarixçə sətrinə iki dəfə klik → həmin sifarişin detal kartı.</summary>
    private async void OnRowDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductHistoryViewModel vm) return;
        var detail = await vm.CreateOrderDetailAsync();
        if (detail is null) return;
        var window = new OrderDetailWindow { DataContext = detail };
        window.Show(this);
    }
}

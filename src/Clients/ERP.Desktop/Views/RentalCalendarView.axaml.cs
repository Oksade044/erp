using Avalonia.Controls;
using Avalonia.Interactivity;
using ERP.Desktop.ViewModels;

namespace ERP.Desktop.Views;

public partial class RentalCalendarView : UserControl
{
    public RentalCalendarView() => InitializeComponent();

    /// <summary>Təqvim sətrinə iki dəfə klik → həmin sifarişin detal kartı.</summary>
    private async void OnRowDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RentalCalendarViewModel vm) return;
        var detail = await vm.CreateOrderDetailAsync();
        if (detail is null) return;

        var window = new OrderDetailWindow { DataContext = detail };
        if (TopLevel.GetTopLevel(this) is Window owner)
            window.Show(owner);
        else
            window.Show();
    }
}

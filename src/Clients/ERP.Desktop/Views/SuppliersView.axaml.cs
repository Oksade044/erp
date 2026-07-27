using Avalonia.Controls;
using Avalonia.Interactivity;
using ERP.Desktop.ViewModels;

namespace ERP.Desktop.Views;

public partial class SuppliersView : UserControl
{
    public SuppliersView() => InitializeComponent();

    private void OnSupplierDoubleTapped(object? sender, RoutedEventArgs e) => OpenLedger();

    private void OnOpenLedger(object? sender, RoutedEventArgs e) => OpenLedger();

    /// <summary>Seçilmiş təchizatçının defter/tarixçə pəncərəsini açır (#15).</summary>
    private void OpenLedger()
    {
        if (DataContext is not SuppliersViewModel vm) return;
        var ledger = vm.CreateLedger();
        if (ledger is null) return;

        var window = new SupplierLedgerWindow { DataContext = ledger };
        if (TopLevel.GetTopLevel(this) is Window owner)
            window.Show(owner);
        else
            window.Show();
    }
}

using Avalonia.Controls;
using ERP.Desktop.ViewModels;

namespace ERP.Desktop.Views;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
        // Sahə icazəsi (customer.debt) — "Borc"/"Val." sütunları yalnız icazə varsa görünür.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not CustomersViewModel vm) return;
            foreach (var col in CustomersGrid.Columns)
                if (col.Header as string is "Borc" or "Val.")
                    col.IsVisible = vm.CanViewDebt;
        };
    }
}

using Avalonia.Controls;
using ERP.Desktop.ViewModels;

namespace ERP.Desktop.Views;

public partial class EmployeesView : UserControl
{
    public EmployeesView()
    {
        InitializeComponent();
        // Sahə icazəsi (employee.salary) — maaş sütunu yalnız icazə varsa görünür.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not EmployeesViewModel vm) return;
            foreach (var col in EmployeesGrid.Columns)
                if (col.Header as string == "Əməkhaqqı")
                    col.IsVisible = vm.CanViewSalary;
        };
    }
}

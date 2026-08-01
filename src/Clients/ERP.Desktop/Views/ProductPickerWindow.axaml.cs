using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ERP.Desktop.Views;

public partial class ProductPickerWindow : Window
{
	public ProductPickerWindow() => InitializeComponent();

	private void OnConfirm(object? sender, RoutedEventArgs e) => Close(true);
	private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}

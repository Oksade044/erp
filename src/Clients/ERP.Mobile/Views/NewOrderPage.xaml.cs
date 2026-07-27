using ERP.Mobile.ViewModels;
using ERP.Shared.Contracts.Customers;
using ERP.Shared.Contracts.Products;

namespace ERP.Mobile.Views;

public partial class NewOrderPage : ContentPage
{
	private readonly NewOrderViewModel _vm;
	public NewOrderPage(NewOrderViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

	private void OnCustomerSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is CustomerDto c) _vm.PickCustomer(c);
		((CollectionView)sender!).SelectedItem = null;
	}

	private async void OnProductSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is ProductDto p) await _vm.PickProductAsync(p);
		((CollectionView)sender!).SelectedItem = null;
	}

	private async void OnDraftLineSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is DraftLine line)
		{
			var ok = await DisplayAlert("Silinsin?", line.Display, "Sil", "İmtina");
			if (ok) _vm.RemoveLine(line);
		}
		((CollectionView)sender!).SelectedItem = null;
	}
}

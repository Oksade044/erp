using ERP.Mobile.ViewModels;
using ERP.Shared.Contracts.Orders;

namespace ERP.Mobile.Views;

public partial class MyOrdersPage : ContentPage
{
	private readonly MyOrdersViewModel _vm;
	public MyOrdersPage(MyOrdersViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}

	// Sətrə toxununca sifariş detalını aç (etibarlı seçim hadisəsi).
	private async void OnOrderSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not OrderDto order) return;
		((CollectionView)sender!).SelectedItem = null; // seçimi sıfırla (təkrar açıla bilsin)
		await _vm.OpenAsync(order);
	}
}

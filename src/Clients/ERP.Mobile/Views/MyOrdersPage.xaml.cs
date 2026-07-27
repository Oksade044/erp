using ERP.Mobile.ViewModels;

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
}

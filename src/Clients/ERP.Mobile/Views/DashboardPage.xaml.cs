using ERP.Mobile.ViewModels;

namespace ERP.Mobile.Views;

public partial class DashboardPage : ContentPage
{
	private readonly DashboardViewModel _vm;
	public DashboardPage(DashboardViewModel vm)
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

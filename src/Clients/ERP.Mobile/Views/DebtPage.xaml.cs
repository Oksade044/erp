using ERP.Mobile.ViewModels;

namespace ERP.Mobile.Views;

public partial class DebtPage : ContentPage
{
	private readonly DebtViewModel _vm;
	public DebtPage(DebtViewModel vm)
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

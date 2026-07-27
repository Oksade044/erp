using ERP.Mobile.ViewModels;

namespace ERP.Mobile.Views;

public partial class FinancePage : ContentPage
{
	private readonly FinanceViewModel _vm;
	public FinancePage(FinanceViewModel vm)
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

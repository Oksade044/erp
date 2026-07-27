using ERP.Mobile.ViewModels;

namespace ERP.Mobile.Views;

public partial class NewOrderPage : ContentPage
{
	public NewOrderPage(NewOrderViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}

using ERP.Mobile.ViewModels;

namespace ERP.Mobile.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}

using ERP.Mobile.ViewModels;

namespace ERP.Mobile.Views;

public partial class ProfilePage : ContentPage
{
	public ProfilePage(ProfileViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}

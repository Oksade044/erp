using ERP.Mobile.ViewModels;

namespace ERP.Mobile.Views;

public partial class OrderDetailPage : ContentPage
{
	public OrderDetailPage(OrderDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}

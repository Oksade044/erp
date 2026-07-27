using ERP.Mobile.Views;

namespace ERP.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		// Detal səhifəsi üçün marşrut (tab-larda deyil, push ilə açılır).
		Routing.RegisterRoute(nameof(OrderDetailPage), typeof(OrderDetailPage));
	}
}

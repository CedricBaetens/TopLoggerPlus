namespace TopLoggerPlus.App;

public partial class AppShell : Shell
{
	public AppShell(AppShellViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		MainShell.GoToAsync($"//{nameof(AccountPage)}");

        Routing.RegisterRoute(nameof(RouteDetailsPage), typeof(RouteDetailsPage));
    }
}
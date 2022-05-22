namespace TopLoggerPlus.App;

public partial class AppShell : Shell
{
	public AppShell(AppShellViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		MainShell.CurrentItem = AllRouteShell;

        Routing.RegisterRoute(nameof(RouteDetailsPage), typeof(RouteDetailsPage));
    }
}
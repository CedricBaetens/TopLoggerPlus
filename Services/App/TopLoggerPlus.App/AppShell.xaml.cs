namespace TopLoggerPlus.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(RouteDetailsPage), typeof(RouteDetailsPage));
	}
}
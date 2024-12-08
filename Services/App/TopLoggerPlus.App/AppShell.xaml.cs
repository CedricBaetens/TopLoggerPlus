namespace TopLoggerPlus.App;

public partial class AppShell : Shell
{
	public AppShell(AppShellViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		MainShell.GoToAsync($"//{nameof(AllRoutesPage)}");

        Routing.RegisterRoute(nameof(RouteDetailsPage), typeof(RouteDetailsPage));
    }
}
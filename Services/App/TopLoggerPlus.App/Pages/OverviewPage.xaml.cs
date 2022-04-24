namespace TopLoggerPlus.App.Pages;

public partial class OverviewPage : ContentPage
{
	public OverviewPage(OverviewViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    private void OnRouteSelected(object sender, SelectedItemChangedEventArgs e)
    {
		var selectedRoute = e.SelectedItem as Route;
    }
}
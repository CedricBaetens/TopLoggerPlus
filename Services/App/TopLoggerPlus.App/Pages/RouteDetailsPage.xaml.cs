namespace TopLoggerPlus.App.Pages;

public partial class RouteDetailsPage : ContentPage
{
	public RouteDetailsPage(RouteDetailsViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ((RouteDetailsViewModel)BindingContext).Appearing.Execute(null);
    }
}
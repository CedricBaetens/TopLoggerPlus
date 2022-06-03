namespace TopLoggerPlus.App.Pages;

public partial class AllRoutesPage : ContentPage
{
	public AllRoutesPage(RouteOverviewViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ((RouteOverviewViewModel)BindingContext).Appearing.Execute("AllRoutes");
    }
    private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        ((RouteOverviewViewModel)BindingContext).Selected.Execute(null);
    }
}
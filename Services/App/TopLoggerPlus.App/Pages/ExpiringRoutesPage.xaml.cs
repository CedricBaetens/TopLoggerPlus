namespace TopLoggerPlus.App.Pages;

public partial class ExpiringRoutesPage : ContentPage
{
    public ExpiringRoutesPage(RouteOverviewViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ((RouteOverviewViewModel)BindingContext).Appearing.Execute("ExpiringRoutes");
    }
    private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        ((RouteOverviewViewModel)BindingContext).Selected.Execute(null);
    }
}
namespace TopLoggerPlus.App.Pages;

public partial class Top10Page : ContentPage
{
    public Top10Page(RouteTop10ViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ((RouteTop10ViewModel)BindingContext).Appearing.Execute(null);
    }
    private void DaysBack_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (BindingContext != null)
            ((RouteTop10ViewModel)BindingContext).DaysBackChanged.Execute(null);
    }
    private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        ((RouteTop10ViewModel)BindingContext).Selected.Execute(null);
    }
}
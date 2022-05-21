namespace TopLoggerPlus.App.Pages;

public partial class AllRoutesPage : ContentPage
{
	public AllRoutesPage(AllRoutesViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        ((AllRoutesViewModel)BindingContext).Selected.Execute(null);
    }
}
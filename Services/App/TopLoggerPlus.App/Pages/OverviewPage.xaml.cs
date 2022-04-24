namespace TopLoggerPlus.App.Pages;

public partial class OverviewPage : ContentPage
{
	public OverviewPage(OverviewViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}
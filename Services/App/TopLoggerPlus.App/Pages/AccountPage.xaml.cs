namespace TopLoggerPlus.App.Pages;

public partial class AccountPage : ContentPage
{
    public AccountPage(AccountViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ((AccountViewModel)BindingContext).Appearing.Execute(null);
    }
}
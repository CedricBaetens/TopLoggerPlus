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
    private void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        ((AccountViewModel)BindingContext).GymSelected.Execute(null);
    }
}
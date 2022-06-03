namespace TopLoggerPlus.App.Pages;

public partial class UserSelectPage : ContentPage
{
    public UserSelectPage(UserSelectViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ((UserSelectViewModel)BindingContext).Appearing.Execute(null);
    }
    private void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        ((UserSelectViewModel)BindingContext).GymSelected.Execute(null);
    }
}
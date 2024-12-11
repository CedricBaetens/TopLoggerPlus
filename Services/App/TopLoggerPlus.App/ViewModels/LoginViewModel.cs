using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TopLoggerPlus.App.Utils;
using TopLoggerPlus.Contracts.Utils;

namespace TopLoggerPlus.App.ViewModels;

public class LoginViewModel(IToploggerService toploggerService, IDialogService dialogService) : INotifyPropertyChanged
{
    private string _refreshToken;
    public string RefreshToken
    {
        get => _refreshToken;
        set
        {
            _refreshToken = value;
            OnPropertyChanged();
        }
    }
    
    public ICommand Login => new Command(async () => await OnLogin());

    private async Task OnLogin()
    {
        if (string.IsNullOrEmpty(RefreshToken)) return;

        try
        {
            await toploggerService.Login(RefreshToken);
            await Shell.Current.GoToAsync($"//{nameof(AccountPage)}");
        }
        catch (AuthenticationFailedException e)
        {
            await dialogService.DisplayAlert("Authentication failed", e.Message);
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
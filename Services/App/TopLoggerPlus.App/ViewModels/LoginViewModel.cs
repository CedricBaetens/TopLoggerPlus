using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TopLoggerPlus.App.Utils;
using TopLoggerPlus.Contracts.Utils;

namespace TopLoggerPlus.App.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly IToploggerService _toploggerService;
    private readonly IDialogService _dialogService;

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
    
    public LoginViewModel(IToploggerService toploggerService, IDialogService dialogService)
    {
        _toploggerService = toploggerService;
        _dialogService = dialogService;
    }
    
    private async Task OnLogin()
    {
        if (string.IsNullOrEmpty(RefreshToken)) return;

        try
        {
            await _toploggerService.Login(RefreshToken);
            await Shell.Current.GoToAsync($"//{nameof(AccountPage)}");
        }
        catch (AuthenticationFailedException e)
        {
            await _dialogService.DisplayAlert("Authentication failed", e.Message);
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
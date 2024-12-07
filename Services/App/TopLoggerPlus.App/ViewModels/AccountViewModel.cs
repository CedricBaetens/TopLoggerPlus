using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TopLoggerPlus.Contracts.Utils;

namespace TopLoggerPlus.App.ViewModels;

public class AccountViewModel : INotifyPropertyChanged
{
    private IToploggerService _toploggerService;

    private User _user;
    public User User
    {
        get => _user;
        set
        {
            _user = value;
            OnPropertyChanged();
        }
    }
    private Gym _selectedGym;
    public Gym SelectedGym
    {
        get => _selectedGym;
        set
        {
            _selectedGym = value;
            OnPropertyChanged();
        }
    }

    public ICommand Appearing => new Command(async () => await OnAppearing());
    public ICommand Logout => new Command(async () => await OnLogout());
    public ICommand ClearData => new Command(async () => await OnClearData());

    public AccountViewModel(IToploggerService toploggerService)
    {
        _toploggerService = toploggerService;
    }

    private async Task OnAppearing()
    {
        try
        {
            User = await _toploggerService.GetMyUserInfo();
            SelectedGym = User.FavoriteGyms?.SingleOrDefault(g => g.Id == User.Gym.Id);
        }
        catch (AuthenticationFailedException)
        {
            await Task.Delay(100);
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
        }
    }
    private async Task OnLogout()
    {
        _toploggerService.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
    private async Task OnClearData()
    {
        _toploggerService.ClearAll();
        await Application.Current.MainPage.DisplayAlert("All info cleared", "", "Ok");
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

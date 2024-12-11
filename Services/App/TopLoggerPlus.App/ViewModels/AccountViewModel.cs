using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TopLoggerPlus.App.Utils;
using TopLoggerPlus.Contracts.Utils;

namespace TopLoggerPlus.App.ViewModels;

public class AccountViewModel(IToploggerService toploggerService, IDialogService dialogService) : INotifyPropertyChanged
{
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

    private async Task OnAppearing()
    {
        try
        {
            User = await toploggerService.GetMyUserInfo();
            SelectedGym = User.FavoriteGyms?.SingleOrDefault(g => g.Id == User.Gym.Id);
        }
        catch (AuthenticationFailedException)
        {
            await Task.Delay(100);
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
        }
        catch (TopLoggerPlusException ex)
        {
            await dialogService.DisplayAlert("Refresh failed", ex.Message);
        }
    }
    private async Task OnLogout()
    {
        toploggerService.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
    private async Task OnClearData()
    {
        toploggerService.ClearAll();
        await dialogService.DisplayAlert("All info cleared");
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

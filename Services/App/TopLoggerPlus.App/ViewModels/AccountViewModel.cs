using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TopLoggerPlus.App.ViewModels;

public class AccountViewModel : INotifyPropertyChanged
{
    private IToploggerService _toploggerService;

    private List<Gym> _gyms;
    public List<Gym> Gyms
    {
        get => _gyms;
        set
        {
            _gyms = value;
            OnPropertyChanged(nameof(Gyms));
        }
    }

    private Gym _selectedGym;
    public Gym SelectedGym
    {
        get => _selectedGym;
        set
        {
            _selectedGym = value;
            OnPropertyChanged(nameof(SelectedGym));
        }
    }

    private List<User> _users;
    public List<User> Users
    {
        get => _users;
        set
        {
            _users = value;
            OnPropertyChanged(nameof(Users));
        }
    }

    private User _selectedUser;
    public User SelectedUser
    {
        get => _selectedUser;
        set
        {
            _selectedUser = value;
            OnPropertyChanged(nameof(SelectedUser));
        }
    }

    public ICommand Appearing => new Command(async () => await OnAppearing());
    public ICommand GymSelected => new Command(async () => await OnGymSelected());
    public ICommand SaveUserInfo => new Command(async () => await OnSaveUserInfo());
    public ICommand Logout => new Command(async () => await OnLogout());
    public ICommand ClearData => new Command(async () => await OnClearData());

    public AccountViewModel(IToploggerService toploggerService)
    {
        _toploggerService = toploggerService;
    }

    private async Task OnAppearing()
    {
        //var stuff = await _toploggerService.GetMyUserInfo();
        Gyms = (await _toploggerService.GetGyms()).OrderBy(g => g.Name).ToList();
        Users = null;
    }
    private async Task OnGymSelected()
    {
        if (SelectedGym == null) return;

        Users = (await _toploggerService.GetUsers(SelectedGym.Id)).OrderBy(u => u.Name).ToList();
        SelectedUser = null;
    }
    private async Task OnSaveUserInfo()
    {
        if (SelectedGym == null || SelectedUser == null) return;

        _toploggerService.SaveUserInfo(SelectedGym, SelectedUser);
        await Application.Current.MainPage.DisplayAlert("UserInfo Saved", "", "Ok");
    }
    private async Task OnLogout()
    {
        //await Application.Current.MainPage.DisplayAlert("Logout pressed", "", "Ok");
        await Shell.Current.GoToAsync($"//{nameof(AllRoutesPage)}");
    }
    private async Task OnClearData()
    {
        _toploggerService.ClearAll();
        await Application.Current.MainPage.DisplayAlert("All info cleared", "", "Ok");
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }
}

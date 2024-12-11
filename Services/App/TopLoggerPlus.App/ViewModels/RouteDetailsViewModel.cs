using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TopLoggerPlus.App.Utils;
using TopLoggerPlus.Contracts.Utils;

namespace TopLoggerPlus.App.ViewModels;

[QueryProperty(nameof(RouteId), nameof(RouteId))]
public class RouteDetailsViewModel(IToploggerService toploggerService, IDialogService dialogService) : INotifyPropertyChanged
{
    private string _routeId;
    public string RouteId
    {
        get => _routeId;
        set
        {
            _routeId = value;
            OnPropertyChanged();
        }
    }

    private Route _route;
    public Route Route
    {
        get => _route;
        set
        {
            _route = value;
            OnPropertyChanged();
        }
    }

    private RouteCommunityInfo _communityInfo;
    public RouteCommunityInfo CommunityInfo
    {
        get => _communityInfo;
        set
        {
            _communityInfo = value;
            OnPropertyChanged();
        }
    }

    public ICommand Appearing => new Command(async () => await OnAppearing());
    public ICommand Back => new Command(async () => await OnBack());

    private async Task OnAppearing()
    {
        try
        {
            Route = toploggerService.GetRouteById(_routeId);
            CommunityInfo = null;
            CommunityInfo = await toploggerService.GetRouteCommunityInfo(_routeId);
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
    private async Task OnBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }
}

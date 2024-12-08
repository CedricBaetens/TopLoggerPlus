using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TopLoggerPlus.Contracts.Utils;

namespace TopLoggerPlus.App.ViewModels;

[QueryProperty(nameof(RouteId), nameof(RouteId))]
public class RouteDetailsViewModel : INotifyPropertyChanged
{
    private readonly IToploggerService _toploggerService;

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

    public RouteDetailsViewModel(IToploggerService toploggerService)
    {
        _toploggerService = toploggerService;
    }

    private async Task OnAppearing()
    {
        try
        {
            Route = _toploggerService.GetRouteById(_routeId);
            CommunityInfo = null;
            CommunityInfo = await _toploggerService.GetRouteCommunityInfo(_routeId);
        }
        catch (AuthenticationFailedException)
        {
            await Task.Delay(100);
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
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

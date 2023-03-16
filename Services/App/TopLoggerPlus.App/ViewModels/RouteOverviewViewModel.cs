using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TopLoggerPlus.App.ViewModels;

public class RouteOverviewViewModel : INotifyPropertyChanged
{
    private readonly IRouteService _routeService;
    private string _filterType;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    private DateTime _lastSynced;
    public DateTime LastSynced
    {
        get => _lastSynced;
        set
        {
            _lastSynced = value;
            OnPropertyChanged(nameof(LastSynced));
        }
    }

    private List<Route> _routes;
    public List<Route> Routes
    {
        get => _routes;
        set
        {
            _routes = value;
            OnPropertyChanged(nameof(Routes));
        }
    }

    private Route _selectedRoute;
    public Route SelectedRoute
    {
        get => _selectedRoute;
        set
        {
            _selectedRoute = value;
            OnPropertyChanged(nameof(SelectedRoute));
        }
    }

    public ICommand Appearing => new Command(async (filterType) => await OnAppearing(filterType?.ToString()));
    public ICommand Refresh => new Command(async () => await OnRefresh());
    public ICommand Selected => new Command(async () => await OnSelected(SelectedRoute));

    public RouteOverviewViewModel(IRouteService routeService)
    {
        _routeService = routeService;
    }

    private async Task OnAppearing(string filterType)
    {
        _filterType = filterType;

        IsBusy = true;
        await ShowRoutes(false);
        IsBusy = false;
    }
    private async Task OnRefresh()
    {
        IsBusy = true;
        await ShowRoutes(true);
        IsBusy = false;
    }
    private async Task OnSelected(Route selectedRoute)
    {
        if (selectedRoute == null) return;

        var navRoute = $"{nameof(RouteDetailsPage)}?{nameof(RouteDetailsViewModel.RouteId)}={selectedRoute.Id}";
        await Shell.Current.GoToAsync(navRoute);
    }

    private async Task ShowRoutes(bool refresh)
    {
        switch (_filterType)
        {
            case "AllRoutes":
                {
                    (var routes, var syncTime) = await _routeService.GetRoutes(refresh);
                    LastSynced = syncTime;
                    Routes = routes?
                                .Where(r => r.Wall.Contains("sector", StringComparison.OrdinalIgnoreCase))
                                .OrderBy(r => r.GradeNumber).ThenBy(r => r.Rope)
                                .ToList();
                }
                break;
            case "ExpiringRoutes":
                {
                    (var routes, var syncTime) = await _routeService.GetRoutes(refresh);
                    LastSynced = syncTime;
                    Routes = routes?
                                .Where(r => r.Wall.Contains("sector", StringComparison.OrdinalIgnoreCase)
                                            && r.Ascends.Count > 0 && r.Ascends.All(a => a.Age > 50))
                                .OrderByDescending(r => r.GradeNumber).ThenBy(r => r.Rope)
                                .ToList();
                }
                break;
            default:
                {
                    Routes = null;
                    await Application.Current.MainPage.DisplayAlert("Route refresh failed", "", "Ok");
                }
                break;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }
}

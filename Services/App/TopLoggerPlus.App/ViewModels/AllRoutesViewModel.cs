using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TopLoggerPlus.App.ViewModels;

public class AllRoutesViewModel : INotifyPropertyChanged
{
    private readonly IRouteService _routeService;

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

    public ICommand Refresh => new Command(async () => await OnRefresh());
    public ICommand Selected => new Command(async () => await OnSelected(SelectedRoute));

    public AllRoutesViewModel(IRouteService routeService)
    {
        _routeService = routeService;
    }

    private async Task OnRefresh()
    {
        IsBusy = true;

        var routes = await _routeService.GetRoutes("klimax", 5437061749);
        if (routes != null)
        {
            Routes = routes
                .Where(r => r.Wall.Contains("sector", StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.GradeNumber).ThenBy(r => r.Rope)
                .ToList();
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Route refresh failed", "", "Ok");
        }

        IsBusy = false;
    }
    private async Task OnSelected(Route selectedRoute)
    {
        if (selectedRoute == null) return;

        var navRoute = $"{nameof(RouteDetailsPage)}?{nameof(RouteDetailsViewModel.RouteId)}={selectedRoute.Id}";
        await Shell.Current.GoToAsync(navRoute);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }
}

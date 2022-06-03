using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TopLoggerPlus.App.ViewModels;

[QueryProperty(nameof(RouteId), nameof(RouteId))]
public class RouteDetailsViewModel : INotifyPropertyChanged
{
    private readonly IRouteService _routeService;

    private int _routeId;
    public int RouteId
    {
        get => _routeId;
        set
        {
            _routeId = value;
            OnPropertyChanged(nameof(RouteId));
        }
    }

    private Route _route;
    public Route Route
    {
        get => _route;
        set
        {
            _route = value;
            OnPropertyChanged(nameof(Route));
        }
    }

    public ICommand Appearing => new Command(() => OnAppearing());
    public ICommand Back => new Command(async () => await OnBack());

    public RouteDetailsViewModel(IRouteService routeService)
    {
        _routeService = routeService;
    }

    private void OnAppearing()
    {
        Route = _routeService.GetRouteById(_routeId);
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

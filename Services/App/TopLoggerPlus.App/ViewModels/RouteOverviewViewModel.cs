using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TopLoggerPlus.App.Utils;
using TopLoggerPlus.Contracts.Utils;

namespace TopLoggerPlus.App.ViewModels;

public class RouteOverviewViewModel(IToploggerService toploggerService, IDialogService dialogService) : INotifyPropertyChanged
{
    private string _filterType;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    private DateTime _lastSynced;
    public DateTime LastSynced
    {
        get => _lastSynced;
        set
        {
            _lastSynced = value;
            OnPropertyChanged();
        }
    }

    private List<Route> _routes;
    public List<Route> Routes
    {
        get => _routes;
        set
        {
            _routes = value;
            OnPropertyChanged();
        }
    }

    private Route _selectedRoute;
    public Route SelectedRoute
    {
        get => _selectedRoute;
        set
        {
            _selectedRoute = value;
            OnPropertyChanged();
        }
    }

    public ICommand Appearing => new Command(async (filterType) => await OnAppearing(filterType?.ToString()));
    public ICommand Refresh => new Command(async () => await OnRefresh());
    public ICommand Selected => new Command(async () => await OnSelected(SelectedRoute));

    private async Task OnAppearing(string filterType)
    {
        _filterType = filterType;

        IsBusy = true;
        try
        {
            await ShowRoutes(false);
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
        IsBusy = false;
    }
    private async Task OnRefresh()
    {
        IsBusy = true;
        try
        {
            await ShowRoutes(true);
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
                    (var routes, var syncTime) = await toploggerService.GetRoutes(refresh);
                    LastSynced = syncTime;
                    Routes = routes?
                        .Where(r => r.Wall.Contains("sector", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(r => r.GradeNumber).ThenBy(r => r.Wall).ThenBy(r => r.Rope)
                        .ToList();
                }
                break;
            case "ExpiringRoutes":
                {
                    (var routes, var syncTime) = await toploggerService.GetRoutes(refresh);
                    LastSynced = syncTime;
                    Routes = routes?
                        .Where(r => r.Wall.Contains("sector", StringComparison.OrdinalIgnoreCase)
                                    && r.AscendsInfo != null && (r.OutAt.HasValue || r.OutPlannedAt.HasValue))
                        .OrderByDescending(r => r.GradeNumber).ThenBy(r => r.Rope)
                        .ToList();
                }
                break;
            default:
                {
                    Routes = null;
                    await dialogService.DisplayAlert("Route refresh failed");
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

﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TopLoggerPlus.App.Utils;
using TopLoggerPlus.Contracts.Utils;

namespace TopLoggerPlus.App.ViewModels;

public class RouteTop10ViewModel(IToploggerService toploggerService, IDialogService dialogService) : INotifyPropertyChanged
{
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

    private int _daysBack;
    public int DaysBack
    {
        get => _daysBack;
        set
        {
            _daysBack = value;
            OnPropertyChanged();
        }
    }

    private string _averageGrade;
    public string AverageGrade
    {
        get => _averageGrade;
        set
        {
            _averageGrade = value;
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

    public ICommand Appearing => new Command(async () => await OnAppearing());
    public ICommand DaysBackChanged => new Command(async () => await OnDaysBackChanged());
    public ICommand Refresh => new Command(async () => await OnRefresh());
    public ICommand Selected => new Command(async () => await OnSelected(SelectedRoute));

    private async Task OnAppearing()
    {
        IsBusy = true;
        try
        {
            if (DaysBack == 0) DaysBack = 60;
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
    private async Task OnDaysBackChanged()
    {
        IsBusy = true;
        await ShowRoutes(false);
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
        var routes = await toploggerService.GetBestAscends(DaysBack, refresh);

        LastSynced = DateTime.Now;
        Routes = routes?
            .Where(r => r.Wall.Contains("sector", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.AscendsInfo?.Score)
            .ThenByDescending(a => a.AscendsInfo?.ToppedFirstAt)
            .Take(10).ToList();
        
        var averageGrade = Math.Ceiling(Routes?.Average(r => r.AscendsInfo?.Score ?? 0) ?? 0);
        var level = Math.Floor(averageGrade / 100);
        (var letter, var remainder) = (averageGrade % 100) switch
        {
            double n when (n < 17) => ("a", n), //0
            double n when (n >= 17 && n < 33) => ("a+", n - 17), //17
            double n when (n >= 33 && n < 50) => ("b", n - 33), //33
            double n when (n >= 50 && n < 67) => ("b+", n - 50), //50
            double n when (n >= 67 && n < 83) => ("c", n - 67), //67
            double n when (n >= 83) => ("c+", n - 83), //83
            _ => ("", 0)
        };
        var percentage = Math.Ceiling(remainder / 0.17);

        AverageGrade = $"Average grade: {level:0}{letter} {percentage}% ({averageGrade})";
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }
}

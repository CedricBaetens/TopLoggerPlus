using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TopLoggerPlus.App.ViewModels;

public class AppShellViewModel : INotifyPropertyChanged
{
	private readonly IRouteService _routeService;

	public ICommand ClearAll => new Command(async () => await OnClearAll());

	public AppShellViewModel(IRouteService routeService)
	{
		_routeService = routeService;
	}

	private async Task OnClearAll()
	{
		_routeService.ClearAll();
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

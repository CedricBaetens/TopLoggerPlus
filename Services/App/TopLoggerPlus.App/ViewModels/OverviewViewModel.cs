using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TopLoggerPlus.App.ViewModels;

public class OverviewViewModel : INotifyPropertyChanged
{
    private readonly IRouteService _routeService;

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

    public ICommand Refresh => new Command(async () => await RefreshData());

    public OverviewViewModel(IRouteService routeService)
    {
        //string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopLoggerPlus", "temp.txt");
        //var exists = File.Exists(fileName);
        //if (!exists)
        //{
        //    var directory = Path.GetDirectoryName(fileName);
        //    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        //    File.WriteAllText(fileName, $"Hello World!! {DateTime.Now}");
        //}
        //else
        //{
        //    var content = File.ReadAllText(fileName);
        //}

        _routeService = routeService;
    }

    private async Task RefreshData()
    {
        Routes = (await _routeService.GetRoutes("klimax", 5437061749))
            .Where(r => r.Wall.Contains("sector", StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.GradeNumber).ThenBy(r => r.Rope)
            .ToList();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }
}

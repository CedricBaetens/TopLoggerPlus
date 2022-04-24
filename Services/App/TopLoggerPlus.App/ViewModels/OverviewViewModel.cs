using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TopLoggerPlus.App.ViewModels;

public class OverviewViewModel : INotifyPropertyChanged
{
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

    public OverviewViewModel()
    {
        string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TopLoggerPlus", "temp.txt");
        var exists = File.Exists(fileName);
        if (!exists)
        {
            var directory = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(fileName, $"Hello World!! {DateTime.Now}");
        }
        else
        {
            var content = File.ReadAllText(fileName);
        }

        Fetch();
    }

    void Fetch()
    {
        Routes = new List<string> { "Item1", "Item2", "Item3" }.Select(r => new Route { Name = r }).ToList();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }
}

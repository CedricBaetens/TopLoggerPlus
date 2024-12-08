using System.Globalization;

namespace TopLoggerPlus.App.Utils;

public class BackgroundColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var route = value as Route;
        return route?.AscendsInfo != null && route.AscendsInfo.TopType > 0
            ? Color.FromArgb("4CAF50")
            : Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

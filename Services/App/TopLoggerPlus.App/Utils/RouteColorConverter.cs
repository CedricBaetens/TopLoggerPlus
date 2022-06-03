using System.Globalization;

namespace TopLoggerPlus.App.Utils;

public class RouteColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (Color.TryParse(value.ToString(), out var color))
            return color;
        return Colors.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

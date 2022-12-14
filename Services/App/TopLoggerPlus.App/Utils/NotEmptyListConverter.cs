﻿using System.Collections;
using System.Globalization;

namespace TopLoggerPlus.App.Utils;

public class NotEmptyListConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ICollection list && list.Count > 0;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

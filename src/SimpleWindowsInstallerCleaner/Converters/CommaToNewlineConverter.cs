using System.Globalization;
using System.Windows.Data;

namespace SimpleWindowsInstallerCleaner.Converters;

public class CommaToNewlineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrEmpty(s))
            return s.Replace(", ", "\n");

        return value ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

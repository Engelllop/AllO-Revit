using System.Globalization;
using System.Windows.Data;

namespace AllO.UI.Converters;

/// <summary>Devuelve la parte del Title tras el guión ("AllO — One Filter" → "One Filter").</summary>
[ValueConversion(typeof(string), typeof(string))]
public class TitleSuffixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value as string ?? string.Empty;
        int i = s.IndexOf('—');
        if (i < 0) i = s.IndexOf('-');
        return i >= 0 ? s.Substring(i + 1).Trim() : s.Trim();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}

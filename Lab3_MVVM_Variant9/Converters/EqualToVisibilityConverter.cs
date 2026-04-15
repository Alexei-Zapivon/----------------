using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lab3_MVVM_Variant9.Converters;

// Конвертер: если значение равно параметру — Visible, иначе Collapsed
public class EqualToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString() ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

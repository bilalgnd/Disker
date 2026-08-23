using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Disker.App.Helpers
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = value is bool b && b;
            if (parameter is string param && param.Equals("inverse", StringComparison.OrdinalIgnoreCase))
            {
                isVisible = !isVisible;
            }
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    public class ProtectionColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isProtected = value is bool b && b;
            return isProtected
                ? new SolidColorBrush(Color.FromRgb(234, 179, 8))   // Amber / Gold
                : new SolidColorBrush(Color.FromRgb(34, 197, 94));  // Emerald Green
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// ISO yazma ilerleme çubuğu için: (yüzde, containerWidth) → piksel genişlik.
    /// MultiBinding ile kullanılır.
    /// </summary>
    public class PercentToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2
                && values[0] is double percent
                && values[1] is double containerWidth
                && containerWidth > 0)
            {
                double width = containerWidth * Math.Clamp(percent, 0, 100) / 100.0;
                return Math.Max(0, width);
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

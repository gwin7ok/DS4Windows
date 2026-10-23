using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DS4WinWPF.DS4Forms.Converters
{
    public class MissingToGrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMissing = value is bool b && b;
            // Not missing: no local value, so the theme's text color applies (it used to be Brushes.Black, which was unreadable on the dark theme)
            return isMissing ? Brushes.Gray : DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

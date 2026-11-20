using System;
using System.Globalization;
using System.Windows.Data;
using DS4Windows.Core.Model;
using DS4Windows.UI.Services;

namespace DS4Windows.UI.Converters
{
    /// <summary><see cref="DS4Color"/> を、XAML の <c>Color</c> 属性（<c>SolidColorBrush.Color</c> など）へ渡せる WPF の色にする。</summary>
    public sealed class DS4ColorToMediaColorConverter : IValueConverter
    {
        public static readonly DS4ColorToMediaColorConverter Instance = new DS4ColorToMediaColorConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is DS4Color color ? MediaColors.ToMedia(color) : System.Windows.DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is System.Windows.Media.Color color ? MediaColors.ToDS4(color) : System.Windows.DependencyProperty.UnsetValue;
    }
}

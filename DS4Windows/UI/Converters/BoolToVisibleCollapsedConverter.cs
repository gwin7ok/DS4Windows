using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DS4Windows.UI.Converters
{
    /// <summary>true なら表示（Visible）、false なら詰めて隠す（Collapsed）。リソースの定義なしで、<c>{x:Static}</c> で使える。</summary>
    public sealed class BoolToVisibleCollapsedConverter : IValueConverter
    {
        public static readonly BoolToVisibleCollapsedConverter Instance = new BoolToVisibleCollapsedConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

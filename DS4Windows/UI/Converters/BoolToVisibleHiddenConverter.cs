using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DS4Windows.UI.Converters
{
    /// <summary>true なら表示（Visible）、false なら場所を残して隠す（Hidden）。リソースの定義なしで、<c>{x:Static}</c> で使える。</summary>
    public sealed class BoolToVisibleHiddenConverter : IValueConverter
    {
        public static readonly BoolToVisibleHiddenConverter Instance = new BoolToVisibleHiddenConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Hidden;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

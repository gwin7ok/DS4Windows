using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DS4Windows.UI.Converters
{
    /// <summary>
    /// 出力ピッカーのボタンの背景。選択中ならテーマの <c>OutputSelectedBrush</c>（無ければ LimeGreen）、そうでなければ <c>SecondaryBrush</c>。
    /// ViewModel が「選ばれているか」の bool だけを持ち、テーマの色の引き当ては View 側で行う。
    /// </summary>
    public sealed class OutputSelectionBrushConverter : IValueConverter
    {
        public static readonly OutputSelectionBrushConverter Instance = new OutputSelectionBrushConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool selected && selected)
                return Application.Current?.TryFindResource("OutputSelectedBrush") as Brush ?? new SolidColorBrush(Colors.LimeGreen);

            return Application.Current?.TryFindResource("SecondaryBrush") as Brush ?? DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

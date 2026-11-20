using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DS4Windows.UI.Converters
{
    /// <summary>
    /// ライトバーの色の表示（プロファイル編集画面）。虹色なら虹の画像、そうでなければメイン色（"#FFRRGGBB"）の単色のブラシにする。
    /// 値は、虹色か（bool）とメイン色の文字列。ViewModel が WPF のブラシや画像を持たないための View 側の部品。
    /// </summary>
    public sealed class LightbarFillConverter : IMultiValueConverter
    {
        public static readonly LightbarFillConverter Instance = new LightbarFillConverter();

        private static ImageBrush? rainbowBrush;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return DependencyProperty.UnsetValue;

            bool rainbow = values[0] is bool b && b;
            if (rainbow)
                return RainbowBrush();

            if (values[1] is string text && ColorConverter.ConvertFromString(text) is Color color)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static ImageBrush RainbowBrush()
        {
            if (rainbowBrush != null) return rainbowBrush;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            // 既定のカラープロファイルを使わない環境でも、同じ色で表示されるようにする
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.UriSource = new Uri($"{ResourcePaths.AssemblyResourcePrefix}component/Resources/rainbowCCrop.png");
            bitmap.EndInit();
            bitmap.Freeze();

            var brush = new ImageBrush(bitmap);
            brush.Freeze();
            rainbowBrush = brush;
            return rainbowBrush;
        }
    }
}

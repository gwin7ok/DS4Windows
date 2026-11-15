using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DS4Windows.UI.Converters
{
    /// <summary>
    /// ファイルのパス（文字列）から、そのファイルに関連付けられたアイコンの画像を作る（View 側の部品）。
    /// ファイルがないとき、パスが空のときは null を返す。ViewModel が画像の型（WPF）を持たず、パスだけを公開するための変換。
    /// </summary>
    public sealed class FileIconConverter : IValueConverter
    {
        public static readonly FileIconConverter Instance = new FileIconConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            using (System.Drawing.Icon ico = System.Drawing.Icon.ExtractAssociatedIcon(path))
            {
                ImageSource icon = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                icon.Freeze();
                return icon;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

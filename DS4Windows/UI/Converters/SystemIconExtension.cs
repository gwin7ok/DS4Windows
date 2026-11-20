#nullable enable

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DS4Windows.UI.Converters
{
    /// <summary>
    /// OS 標準のアイコン（<c>Shield</c>、<c>Question</c> など。<see cref="SystemIcons"/> の名前）を画像として返す XAML の拡張。
    /// 例: <c>Source="{localConverters:SystemIcon Shield}"</c>
    /// </summary>
    public sealed class SystemIconExtension : MarkupExtension
    {
        public SystemIconExtension(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            Icon icon = Name switch
            {
                "Shield" => SystemIcons.Shield,
                "Question" => SystemIcons.Question,
                "Information" => SystemIcons.Information,
                "Warning" => SystemIcons.Warning,
                "Error" => SystemIcons.Error,
                _ => throw new ArgumentException($"未対応のシステムアイコン: {Name}"),
            };

            // 旧実装と同じく、ビットマップのハンドルから作る（アイコンのハンドルだと透過の扱いが変わる）
            using (Bitmap bitmap = icon.ToBitmap())
            {
                ImageSource source = Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
        }
    }
}

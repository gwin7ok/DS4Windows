using System.Windows.Media;
using DS4Windows.Core.Model;

namespace DS4Windows.UI.Services
{
    /// <summary>ViewModel が持つ <see cref="DS4Color"/> と、画面（WPF）の <see cref="Color"/> の相互変換（View 側の部品。状態を持たない）。</summary>
    public static class MediaColors
    {
        public static Color ToMedia(DS4Color color) => Color.FromArgb(255, color.red, color.green, color.blue);

        public static DS4Color ToDS4(Color color) => new DS4Color(color.R, color.G, color.B);
    }
}

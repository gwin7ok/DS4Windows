using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit;

namespace DS4Windows.UI
{
    /// <summary>
    /// プロファイル編集画面の「その他」タブの一般項目の中身。DataContext は
    /// <see cref="DS4Windows.UI.ViewModels.GeneralSettingsViewModel"/>。
    /// 入力の表示の開始・停止（タイマー）とプログラムの選択画面は、画面の外（<c>ProfileEditor</c>）が持つため、
    /// この部品は押されたことを知らせるだけにする。
    /// </summary>
    public partial class GeneralSettingsPanel : UserControl
    {
        /// <summary>「入力の表示にコントローラーを使う」のチェックが押された。</summary>
        public event RoutedEventHandler UseControllerReadoutClick;

        /// <summary>「プロファイルと一緒に起動するプログラム」の参照ボタンが押された。</summary>
        public event RoutedEventHandler LaunchProgramBrowseClick;

        public GeneralSettingsPanel()
        {
            InitializeComponent();
        }

        /// <summary>入力の表示に使うコントローラーの番号（表示のみ）。</summary>
        public IntegerUpDown UseControllerUD => useControllerUD;

        private void UseControllerReadoutCk_Click(object sender, RoutedEventArgs e)
        {
            UseControllerReadoutClick?.Invoke(sender, e);
        }

        private void LaunchProgBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            LaunchProgramBrowseClick?.Invoke(sender, e);
        }
    }
}

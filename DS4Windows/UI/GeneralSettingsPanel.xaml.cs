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

        /// <summary>「入力の表示にコントローラーを使う」のチェック（規則 V3）。押されたことを <see cref="UseControllerReadoutClick"/> で知らせる。</summary>
        public System.Windows.Input.ICommand UseControllerReadoutCommand { get; }

        /// <summary>参照ボタン（規則 V3）。押されたことを <see cref="LaunchProgramBrowseClick"/> で知らせる。</summary>
        public System.Windows.Input.ICommand LaunchProgramBrowseCommand { get; }

        public GeneralSettingsPanel()
        {
            UseControllerReadoutCommand = DS4Windows.UI.ViewModels.Util.CommandGuard.CreateSync(
                () => UseControllerReadoutClick?.Invoke(this, new RoutedEventArgs()), name: nameof(UseControllerReadoutCommand));
            LaunchProgramBrowseCommand = DS4Windows.UI.ViewModels.Util.CommandGuard.CreateSync(
                () => LaunchProgramBrowseClick?.Invoke(this, new RoutedEventArgs()), name: nameof(LaunchProgramBrowseCommand));
            InitializeComponent();
        }

        /// <summary>入力の表示に使うコントローラーの番号（表示のみ）。</summary>
        public IntegerUpDown UseControllerUD => useControllerUD;

    }
}

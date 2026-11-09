using System;
using System.Windows;
using System.Windows.Controls;

namespace DS4Windows.UI
{
    /// <summary>
    /// プロファイル編集画面の「その他」タブの「仮想コントローラー」と「振動」の中身。DataContext は
    /// <see cref="DS4Windows.UI.ViewModels.OutputSettingsViewModel"/>。
    /// 実機を動かす振動のテストと、出力の種類の変更に伴うマッピング一覧の更新は、画面の外（<c>ProfileEditor</c>）が持つため、
    /// この部品は押されたことと選択が変わったことを知らせるだけにする。
    /// </summary>
    public partial class OutputSettingsPanel : UserControl
    {
        /// <summary>振動のテストのボタン（「強」「弱」）が押された。sender はボタン（<see cref="HeavyRumbleTestBtn"/> か <see cref="LightRumbleTestBtn"/>）。</summary>
        public event RoutedEventHandler RumbleTestClick;

        public OutputSettingsPanel()
        {
            InitializeComponent();
        }

        /// <summary>エミュレートするコントローラーの種類の選択。</summary>
        public ComboBox OutConTypeCombo => outConTypeCombo;

        public Button HeavyRumbleTestBtn => heavyRumbleTestBtn;

        public Button LightRumbleTestBtn => lightRumbleTestBtn;

        private void RumbleTestBtn_Click(object sender, RoutedEventArgs e)
        {
            RumbleTestClick?.Invoke(sender, e);
        }
    }
}

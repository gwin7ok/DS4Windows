using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// プロファイル編集画面の「ジャイロ」タブの中身。DataContext は <see cref="GyroSettingsViewModel"/>。
    /// 割り当て編集画面を開く操作と、実機のジャイロの校正は、編集画面の全体に関わるため、イベントで編集画面へ知らせる。
    /// </summary>
    public partial class GyroSettingsPanel : UserControl
    {
        private GyroSettingsViewModel viewModel;

        public GyroSettingsPanel()
        {
            InitializeComponent();
        }

        // 割り当ての一覧のホバー表示が、傾きのボタンを目印に使う
        public Button TiltZNegButton => gyroZNBtn;
        public Button TiltZPosButton => gyroZPBtn;
        public Button TiltXNegButton => gyroXNBtn;
        public Button TiltXPosButton => gyroXPBtn;

        /// <summary>傾き（Tilt）のボタンが押された。引数は対象のコントロール。</summary>
        public event EventHandler<DS4Controls> TiltControlClicked;

        /// <summary>ジャイロの校正（リセット）が求められた。</summary>
        public event EventHandler CalibrationRequested;

        /// <summary>トリガーのメニューの項目を作る。画面を作るときに 1 回だけ呼ぶ。</summary>
        public void CreateTriggerMenus(GyroSettingsViewModel gyro)
        {
            viewModel = gyro ?? throw new ArgumentNullException(nameof(gyro));
            gyro.CreateGyroTriggerMenuItems(gyroControlsTrigBtn.ContextMenu, GyroControlsMenuItem_Click);
            gyro.CreateGyroTriggerMenuItems(gyroMouseTrigBtn.ContextMenu, GyroMouseTrigMenuItem_Click);
            gyro.CreateGyroTriggerMenuItems(gyroMouseStickTrigBtn.ContextMenu, GyroMouseStickTrigMenuItem_Click);
        }

        /// <summary>保存されているトリガーを、メニューのチェックと表示へ反映する（プロファイルの読み込み・プリセットの適用のあと）。</summary>
        public void PopulateTriggerMenus(GyroSettingsViewModel gyro)
        {
            viewModel = gyro ?? throw new ArgumentNullException(nameof(gyro));
            gyro.PopulateGyroMouseTrig(gyroMouseTrigBtn.ContextMenu);
            gyro.PopulateGyroMouseStickTrig(gyroMouseStickTrigBtn.ContextMenu);
            gyro.PopulateGyroControlsTrig(gyroControlsTrigBtn.ContextMenu);
        }

        /// <summary>傾きの 4 方向の割り当ての表示を、割り当ての一覧の各コントロールへ結ぶ。</summary>
        public void AssignTiltAssociation(MappingListViewModel mappingList)
        {
            gyroZNLb.DataContext = mappingList.ControlMap[DS4Controls.GyroZNeg];
            gyroZPLb.DataContext = mappingList.ControlMap[DS4Controls.GyroZPos];
            gyroXNLb.DataContext = mappingList.ControlMap[DS4Controls.GyroXNeg];
            gyroXLb.DataContext = mappingList.ControlMap[DS4Controls.GyroXPos];
        }

        private void TiltControlsButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            TiltControlClicked?.Invoke(this, (DS4Controls)Convert.ToInt32(btn.Tag));
        }

        private void GyroCalibration_Click(object sender, RoutedEventArgs e)
        {
            CalibrationRequested?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseTrigBtn_Click(object sender, RoutedEventArgs e)
        {
            gyroMouseTrigBtn.ContextMenu.IsOpen = true;
        }

        private void GyroMouseStickTrigBtn_Click(object sender, RoutedEventArgs e)
        {
            gyroMouseStickTrigBtn.ContextMenu.IsOpen = true;
        }

        private void GyroControlsTrigBtn_Click(object sender, RoutedEventArgs e)
        {
            gyroControlsTrigBtn.ContextMenu.IsOpen = true;
        }

        private void GyroMouseTrigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = gyroMouseTrigBtn.ContextMenu;
            MenuItem alwaysOnItem = menu.Items[menu.Items.Count - 1] as MenuItem;
            viewModel.UpdateGyroMouseTrig(menu, e.OriginalSource == alwaysOnItem);
        }

        private void GyroMouseStickTrigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = gyroMouseStickTrigBtn.ContextMenu;
            MenuItem alwaysOnItem = menu.Items[menu.Items.Count - 1] as MenuItem;
            viewModel.UpdateGyroMouseStickTrig(menu, e.OriginalSource == alwaysOnItem);
        }

        private void GyroControlsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = gyroControlsTrigBtn.ContextMenu;
            MenuItem alwaysOnItem = menu.Items[menu.Items.Count - 1] as MenuItem;
            viewModel.UpdateGyroControlsTrig(menu, e.OriginalSource == alwaysOnItem);
        }
    }
}

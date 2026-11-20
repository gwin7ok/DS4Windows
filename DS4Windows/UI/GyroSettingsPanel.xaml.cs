using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Model;
using DS4Windows.UI.Services;
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

        public System.Windows.Input.ICommand TiltControlCommand { get; }

        public System.Windows.Input.ICommand CalibrationCommand { get; }

        public GyroSettingsPanel()
        {
            TiltControlCommand = DS4Windows.UI.ViewModels.Util.CommandGuard.CreateSync<object>(parameter => TiltControlClicked?.Invoke(this, (DS4Controls)Convert.ToInt32(parameter)), name: nameof(TiltControlCommand));
            CalibrationCommand = DS4Windows.UI.ViewModels.Util.CommandGuard.CreateSync(() => CalibrationRequested?.Invoke(this, EventArgs.Empty), name: nameof(CalibrationCommand));
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
            ArgumentNullException.ThrowIfNull(gyro);
            viewModel = gyro;
            TriggerMenuAdapter.AddItems(gyroControlsTrigBtn.ContextMenu, gyro.TriggerItemNames, GyroControlsMenuItem_Click);
            TriggerMenuAdapter.AddItems(gyroMouseTrigBtn.ContextMenu, gyro.TriggerItemNames, GyroMouseTrigMenuItem_Click);
            TriggerMenuAdapter.AddItems(gyroMouseStickTrigBtn.ContextMenu, gyro.TriggerItemNames, GyroMouseStickTrigMenuItem_Click);
        }

        /// <summary>保存されているトリガーを、メニューのチェックと表示へ反映する（プロファイルの読み込み・プリセットの適用のあと）。</summary>
        public void PopulateTriggerMenus(GyroSettingsViewModel gyro)
        {
            ArgumentNullException.ThrowIfNull(gyro);
            viewModel = gyro;
            Populate(gyroMouseTrigBtn.ContextMenu, gyro.PopulateGyroMouseTrig);
            Populate(gyroMouseStickTrigBtn.ContextMenu, gyro.PopulateGyroMouseStickTrig);
            Populate(gyroControlsTrigBtn.ContextMenu, gyro.PopulateGyroControlsTrig);
        }

        /// <summary>傾きの 4 方向の割り当ての表示を、割り当ての一覧の各コントロールへ結ぶ。</summary>
        public void AssignTiltAssociation(MappingListViewModel mappingList)
        {
            gyroZNLb.DataContext = mappingList.ControlMap[DS4Controls.GyroZNeg];
            gyroZPLb.DataContext = mappingList.ControlMap[DS4Controls.GyroZPos];
            gyroXNLb.DataContext = mappingList.ControlMap[DS4Controls.GyroXNeg];
            gyroXLb.DataContext = mappingList.ControlMap[DS4Controls.GyroXPos];
        }

        // メニューのチェックを ViewModel の項目へ写して計算させ、結果をメニューへ書き戻す
        private static void Populate(ContextMenu menu, Action<IReadOnlyList<TriggerMenuEntry>> populate)
        {
            var entries = TriggerMenuAdapter.Read(menu);
            populate(entries);
            TriggerMenuAdapter.Write(menu, entries);
        }

        private static void Update(ContextMenu menu, MenuItem alwaysOnItem, RoutedEventArgs e, Action<IReadOnlyList<TriggerMenuEntry>, bool> update)
        {
            var entries = TriggerMenuAdapter.Read(menu);
            update(entries, e.OriginalSource == alwaysOnItem);
            TriggerMenuAdapter.Write(menu, entries);
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
            Update(menu, alwaysOnItem, e, viewModel.UpdateGyroMouseTrig);
        }

        private void GyroMouseStickTrigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = gyroMouseStickTrigBtn.ContextMenu;
            MenuItem alwaysOnItem = menu.Items[menu.Items.Count - 1] as MenuItem;
            Update(menu, alwaysOnItem, e, viewModel.UpdateGyroMouseStickTrig);
        }

        private void GyroControlsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = gyroControlsTrigBtn.ContextMenu;
            MenuItem alwaysOnItem = menu.Items[menu.Items.Count - 1] as MenuItem;
            Update(menu, alwaysOnItem, e, viewModel.UpdateGyroControlsTrig);
        }
    }
}

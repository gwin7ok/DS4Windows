using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Profiles;
using DS4Windows.UI.ViewModels;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace DS4Windows.UI
{
    /// <summary>プリセット・スティックの較正・左の編集欄の開閉。</summary>
    public partial class ProfileEditor
    {
        private void MappingListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mappingListVM.SelectedIndex >= 0)
            {
                if (reverseHoverIndexes.TryGetValue(mappingListVM.SelectedIndex, out Button? tempBtn))
                {
                    InputControlHighlight(tempBtn);
                }
            }

        }

        /// <summary>編集画面を、左ペイン全体に重ねて表示する（タブを隠すため、編集中は、左ペインのタブを切り替えられない）。</summary>
        private void OpenLeftPaneEditor(UserControl editor)
        {
            // 左の列の幅は Auto（中身で決まる）ため、タブを隠しても、列の幅が変わらないよう、いまの幅に固定する
            controlEditorHost.Width = sidebarTabControl.ActualWidth;
            leftPaneEditor = editor;
            sidebarTabControl.Visibility = Visibility.Collapsed;
            controlEditorHost.Children.Add(editor);
            editor.Visibility = Visibility.Visible;
            controlEditorHost.Visibility = Visibility.Visible;
        }

        private void CloseLeftPaneEditor()
        {
            if (leftPaneEditor == null) return;

            controlEditorHost.Children.Remove(leftPaneEditor);
            leftPaneEditor = null;
            controlEditorHost.Visibility = Visibility.Collapsed;
            controlEditorHost.ClearValue(WidthProperty);
            sidebarTabControl.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 割り当てエディターを、左ペイン全体に重ねて表示する。保存したら、従来と同じ更新（割り当て名、ハイライトのラベル、カスタムの印）を行う。
        /// キャンセルは、何もしない（編集用のコピーを捨てる）。従来の選択ダイアログ（BindingWindow の Full のモード）は、使わない。
        /// </summary>
        private void ShowControlAssignmentEditor(MappedControl mpControl, bool updateHighlight)
        {
            if (mpControl == null || leftPaneEditor != null) return;

            ProfileList profList = (Application.Current.MainWindow as MainWindow)!.ProfileListHolder;   // 画面が開いているあいだ、メイン画面は必ずある
            AssignmentEditor editor = editorWindowFactory.CreateControlActionEditor(deviceNum, mpControl, profList, targetDevice);
            editor.ControlSaved += (sender2, args) =>
            {
                CloseLeftPaneEditor();
                mpControl.UpdateMappingName();
                if (updateHighlight)
                {
                    UpdateHighlightLabel(mpControl);
                }

                profileSettingsVM.RefreshCustomsFlags();
            };
            editor.Cancel += (sender2, args) => CloseLeftPaneEditor();

            OpenLeftPaneEditor(editor);
        }

        // Controls タブの全ての割り当て（L2／R2 Full Pull、スティックの Outer Bind を含む一覧の全項目）を Default に戻す。
        // Extras、マクロも含めて、そのボタンの設定を初期化する（DS4ControlSettings.Reset）。プロファイルは Save するまで保存されない
        private void ResetAllControls()
        {
            MessageBoxResult confirm = MessageBox.Show(
                DS4Windows.Translations.Strings.ResetAllControlsConfirm,
                "DS4Windows", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes) return;

            foreach (MappedControl mpControl in mappingListVM.Mappings)
            {
                mpControl.ResetSetting();
            }

            profileSettingsVM.RefreshCustomsFlags();
            highlightControlDisplayLb.Content = "";
            AppLogger.LogUser(AppLogLevel.Info, "Log_Editor_ControlsReset");
        }

        private void SidebarTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 左ペインのタブの中の ComboBox や入れ子の TabControl（Touchpad・ジャイロの Output Mode ほか）の選択変更も、
            // このイベントとして届く。左ペインのタブ自身の選択変更だけを扱う
            if (e.OriginalSource != sidebarTabControl) return;

            AppLogger.Log(AppLogLevel.Debug, $"[SidebarTabControl_SelectionChanged] SelectedItem={sidebarTabControl.SelectedItem?.GetType().Name}, SelectedIndex={sidebarTabControl.SelectedIndex}, IsLoaded={sidebarTabControl.IsLoaded}, IsVisible={sidebarTabControl.IsVisible}, Added={e.AddedItems?.Count}, Removed={e.RemovedItems?.Count}");
            // スペシャルアクションのタブを最初に開いたときに、一度だけ並べ替えを適用する
            if (!specialActionsSorted && sidebarTabControl.SelectedItem == specialActionsTab)
            {
                specialActionsSorted = true;
                SortSpecialActionsList(specialActionsVM.CurrentSortColumn, specialActionsVM.CurrentSortAscending);
            }

            if (sidebarTabControl.SelectedItem == contReadingsTab)
            {
                controllerReadingsTabActive = true;
                conReadingsUserCon.EnableControl(true);
            }
            else if (controllerReadingsTabActive)
            {
                controllerReadingsTabActive = false;
                conReadingsUserCon.EnableControl(false);
            }
        }

        private void PresetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 項目ごとの Click ではなく、ContextMenu で受ける（MenuItem.Click は上へ伝わる）。
            // 項目を ContextMenu のリソースに置くと、コンパイル時の接続番号と読み込み時の順序がずれて、画面を開く時に落ちるため。
            MenuItem? item = e.OriginalSource as MenuItem;
            if (item == null || item.HasItems)
                return;
            int baseTag = Convert.ToInt32(item.Tag);
            int subTag = Convert.ToInt32(item.CommandParameter);
            if (baseTag >= 0 && subTag >= 0)
            {
                List<DS4Controls> controls =
                    profileSettingsVM.PresetMenuUtil.ModifySettingWithPreset(baseTag, subTag);
                foreach (DS4Controls control in controls)
                {
                    MappedControl mpControl = mappingListVM.ControlMap[control];
                    mpControl.UpdateMappingName();
                }

                profileSettingsVM.RefreshCustomsFlags();
                highlightControlDisplayLb.Content = "";
            }
        }

        private void OpenPresetOptions()
        {
            sidebarTabControl.SelectedIndex = 0;

            PresetOptionWindow presetWin = editorWindowFactory.CreatePresetOptionWindow();
            presetWin.SetupData(deviceNum);
            presetWin.ToPresetsScreen();
            presetWin.DelayPresetApply = true;
            presetWin.ShowDialog();

            if (presetWin.Result == MessageBoxResult.OK)
            {
                StopEditorBindings();
                presetWin.ApplyPreset();
                RefreshEditorBindings();
            }
        }

        private void CalibrateStick_OnClick(object? sender, int tag)
        {
            // 校正には実機が必要。旧判定 deviceNum == 8（一覧経由）は、編集スロットが常に 8 になったため
            // 実機の有無（targetDevice）の判定に置き換えた。メッセージの文言は維持
            if (!HasTargetDevice)
            {
                MessageBox.Show("Stick recalibration is only available if the profile editor is opened " +
                                "with the Edit button next to the controller you want to recalibrate in the main " +
                                "window.",
                    "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var stick = tag switch
            {
                0 => Stick.Left,
                1 => Stick.Right,
                _ => throw new IndexOutOfRangeException("Wrong stick index. Must be 0 for left or 1 for right.")
            };

            // 実機（targetDevice）の現在の入力を読み、補正値は profileSettingsVM 経由で作業スロットへ書く
            StickCalibrationWindow window = editorWindowFactory.CreateStickCalibrationWindow(stick, targetDevice, profileSettingsVM);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }
    }
}

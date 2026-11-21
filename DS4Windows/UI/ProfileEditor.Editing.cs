using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Services;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DS4Windows.UI
{
    /// <summary>プロファイルの読み込み・保存・取り消しと、画面の割り当ての更新。</summary>
    public partial class ProfileEditor
    {
        /// <summary>
        /// 編集するプロファイルを作業スロット（Global.TEST_PROFILE_INDEX）へ読み込み、画面を初期化する。
        /// 読み込み先は常に作業スロットで、コントローラーのスロットには触れない。
        /// 実機（targetDevice）はコンストラクタで確定しているため、引数では受け取らない。
        /// </summary>
        /// <param name="profile">編集するプロファイル。null は新規作成（プリセット選択から始める）</param>
        public void Reload(ProfileEntity? profile = null)
        {
            ClearEditorDataContexts();

            AppLogger.LogDebug($"[ProfileEditor] Reload: editSlot={deviceNum}, targetDevice={targetDevice}, profile={(profile != null ? profile.Name : "(new profile)")}");

            if (profile != null)
            {
                currentProfile = profile;
                backend.LoadProfileForEditing(deviceNum, profile.Name ?? string.Empty);
                // 廃止された「シフト」「ジャイロスワイプ」の設定が残っていれば、警告して、削除するか残すかを選ばせる（ViewModel が判断する）
                profileSettingsVM.ApplyRemovedSettingNotices(profile.Name ?? string.Empty);
                profileNameTxt.Text = profile.Name;
                profileNameTxt.IsEnabled = false;
            }
            else
            {
                currentProfile = null;
                PresetOptionWindow presetWin = editorWindowFactory.CreatePresetOptionWindow();
                presetWin.SetupData(deviceNum);
                presetWin.ShowDialog();
                if (presetWin.Result == MessageBoxResult.Cancel)
                {
                    profileSettingsVM.LoadBlankProfile();
                }
            }

            // プロファイルロード完了後に、XMLから読み込まれた ProfileActions を使ってリストを初期化・復元
            string currentProfileActions = backend.GetEnabledActionsText(deviceNum);
            specialActionsVM.LoadActions(currentProfile == null, currentProfileActions);

            lightbarPanel.RefreshColorByBatteryLayout(profileSettingsVM.Lightbar);

            // Controller Readings は、実機（なければコントローラー0）の入力に対して、
            // 作業スロット（編集中の設定）で計算した結果を表示する
            int readingsDevice = profileSettingsVM.FuncDevNum;
            generalPanel.UseControllerUD.Value = readingsDevice + 1;
            conReadingsUserCon.UseDevice(readingsDevice, ControllerLimits.TestProfileIndex);
            contReadingsTab.IsEnabled = true;

            conReadingsUserCon.EnableControl(false);
            sticksPanel.AxialLeft.UseDevice(_viewModelFactory.CreateAxialStickControlViewModel(profileSettingsVM.Sticks.Left.Mod));
            sticksPanel.AxialRight.UseDevice(_viewModelFactory.CreateAxialStickControlViewModel(profileSettingsVM.Sticks.Right.Mod));

            mappingListVM.UpdateMappings();
            profileSettingsVM.UpdateLateProperties();
            // mappingListVM はプロファイル読み込み前の出力種別で生成されるため、読み込み後の
            // 出力種別（Xbox 360／DS4）でボタン名表記を明示的に揃える（出力の種類の選択の SelectionChanged の発生有無に依存しない）
            mappingListVM.UpdateMappingDevType(profileSettingsVM.Output.ContType);
            touchpadPanel.PopulateDisInvertMenu(profileSettingsVM.Touchpad);
            gyroPanel.PopulateTriggerMenus(profileSettingsVM.Gyro);
            AssignEditorDataContexts();
            // 読み込んだプロファイルのタッチパッド出力モードに合わせて、Button Activation Mode の表示位置を揃える
            // （コンストラクタでの配置は読み込み前の値で行われ、TouchpadOutputIndex の setter を通らない変更では更新されない）
            TouchpadButtonControlDisplaySetup();

            // 入力の表示へ渡す値の判断は ReadingsDeadZoneSnapshot が行い、ここは値を渡すだけにする
            var dead = ReadingsDeadZoneSnapshot.Create(profileSettingsVM, sticksPanel.AxialLeft.AxialVM, sticksPanel.AxialRight.AxialVM);
            if (dead.LsDeadX is { } lsDeadX && dead.LsDeadY is { } lsDeadY) { conReadingsUserCon.LsDeadX = lsDeadX; conReadingsUserCon.LsDeadY = lsDeadY; }
            if (dead.RsDeadX is { } rsDeadX && dead.RsDeadY is { } rsDeadY) { conReadingsUserCon.RsDeadX = rsDeadX; conReadingsUserCon.RsDeadY = rsDeadY; }
            conReadingsUserCon.LsDeadAxial = profileSettingsVM.Sticks.Left.IsAxialDeadZone;
            conReadingsUserCon.RsDeadAxial = profileSettingsVM.Sticks.Right.IsAxialDeadZone;
            conReadingsUserCon.L2Dead = dead.L2Dead;
            conReadingsUserCon.R2Dead = dead.R2Dead;
            conReadingsUserCon.SixAxisXDead = dead.SixAxisXDead;
            conReadingsUserCon.SixAxisZDead = dead.SixAxisZDead;
            conReadingsUserCon.LsDriftX = dead.LsDriftX;
            conReadingsUserCon.LsDriftY = dead.LsDriftY;
            conReadingsUserCon.RsDriftX = dead.RsDriftX;
            conReadingsUserCon.RsDriftY = dead.RsDriftY;

            sticksPanel.AxialLeft.AxialVM.PropertyChanged += AxialStick_PropertyChanged;
            sticksPanel.AxialRight.AxialVM.PropertyChanged += AxialStick_PropertyChanged;

            // 初期表示も統一ソートメソッドを使用
            // NOTE: 呼び出しを削除しました。レンダリング完了後に Dispatcher.BeginInvoke で
            //       `SortSpecialActionsList(currentSortColumn, currentSortAsc)` を実行して
            //       ヘッダー内の DataTemplate 部分が確実に生成されてから ▲/▼ を反映します。

            if (profileSettingsVM.General.UseControllerReadout)
            {
                inputTimer?.Start();
            }
        }

        /// <summary>
        /// 設定を読み直す前に、各タブの DataContext を外す。ViewModel の読み直しは PropertyChanged を出さずに値を入れ替えるため、
        /// 外してから <see cref="AssignEditorDataContexts"/> で付け直すことで、画面へ反映する（<c>UpdateLateProperties</c> の規約）。
        /// </summary>
        private void ClearEditorDataContexts()
        {
            profileSettingsTabCon.DataContext = null;
            touchpadTab.DataContext = null;
            gyroTab.DataContext = null;
            mappingListBox.DataContext = null;
            specialActionsTab.DataContext = null;
            lightbarRect.DataContext = null;
        }

        /// <summary>読み直しのあと、各タブの DataContext を付け直す（<see cref="ClearEditorDataContexts"/> の対）。</summary>
        private void AssignEditorDataContexts()
        {
            profileSettingsTabCon.DataContext = profileSettingsVM;
            touchpadTab.DataContext = profileSettingsVM.Touchpad;
            gyroTab.DataContext = profileSettingsVM.Gyro;
            mappingListBox.DataContext = mappingListVM;
            specialActionsTab.DataContext = specialActionsVM;
            lightbarRect.DataContext = profileSettingsVM.Lightbar;
        }

        private void RefreshEditorBindings()
        {
            // 注意: profileSettingsVM は INotifyPropertyChanged 非実装のため、直後のDataContext代入
            // （同一インスタンスの再代入）だけではWPFのバインディングは更新されない。本メソッドの
            // 呼び出し元（PresetBtn_Click）が必ず先にStopEditorBindings()（DataContext=null）を
            // 呼んでいることが、UI反映を成立させる前提条件になっている。詳細はUpdateLateProperties()の
            // コメント、および docs-forDIMG/MadeByAgent/Archive/Phase5-Step14-FormSettings-Unification-Status.md タスク(c)-1 を参照。
            specialActionsVM.LoadActions(currentProfile == null);
            mappingListVM.UpdateMappings();
            profileSettingsVM.UpdateLateProperties();
            // プリセット適用後の出力種別でボタン名表記を明示的に揃える（Reload と同じ理由）
            mappingListVM.UpdateMappingDevType(profileSettingsVM.Output.ContType);
            touchpadPanel.PopulateDisInvertMenu(profileSettingsVM.Touchpad);
            gyroPanel.PopulateTriggerMenus(profileSettingsVM.Gyro);
            AssignEditorDataContexts();
            touchButtonUC.RegisterDataContext();
            // プリセット適用でタッチパッド出力モードが変わっても、Button Activation Mode の表示位置が追従するようにする
            TouchpadButtonControlDisplaySetup();

            conReadingsUserCon.LsDeadX = profileSettingsVM.Sticks.Left.DeadZone;
            conReadingsUserCon.RsDeadX = profileSettingsVM.Sticks.Right.DeadZone;
            conReadingsUserCon.L2Dead = profileSettingsVM.Triggers.L2DeadZone;
            conReadingsUserCon.R2Dead = profileSettingsVM.Triggers.R2DeadZone;
            conReadingsUserCon.SixAxisXDead = profileSettingsVM.SixAxis.SXDeadZone;
            conReadingsUserCon.SixAxisZDead = profileSettingsVM.SixAxis.SZDeadZone;
            conReadingsUserCon.LsDriftX = profileSettingsVM.Sticks.Left.DriftXAxis;
            conReadingsUserCon.LsDriftY = profileSettingsVM.Sticks.Left.DriftYAxis;
            conReadingsUserCon.RsDriftX = profileSettingsVM.Sticks.Right.DriftXAxis;
            conReadingsUserCon.RsDriftY = profileSettingsVM.Sticks.Right.DriftYAxis;
        }

        private void CancelEdit()
        {
            backend.StopRumble(profileSettingsVM.FuncDevNum);

            profileSettingsVM.Output.ResetOutDevTypeTemp();
            // 編集は作業スロットに対して行っており、コントローラーのスロットには届いていないため、
            // 元のプロファイルを読み込み直して取り消す処理は不要。
            // 作業スロットは次に編集画面を開いたときに読み込み直される
            AppLogger.LogDebug($"[ProfileEditor] Cancel: edit slot {deviceNum} discarded; controller slots were not modified (targetDevice={targetDevice})");

            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// プロファイル保存および適用の共通処理ルート。
        /// UI上の選択値をSSOTに反映してXMLに保存し、親画面へ保存完了通知（ProfileSaved）を送信します。
        /// </summary>
        /// <param name="isApply">保存（false）か適用（true）か</param>
        /// <returns>保存成否</returns>
        private bool ExecuteSaveOrApply(bool isApply = false)
        {
            string profileName = profileNameTxt.Text.Trim();
            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show(Properties.Resources.ValidName, "DS4Windows",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            // SpecialActionsListViewModel から有効なアクション名リストを取得し、Global.ProfileActions に確実に反映
            if (specialActionsVM != null)
            {
                specialActionsVM.CommitEnabledActionsToProfile();
            }

            SaveSplitterAndColumnWidths();

            // profileRepository はコンストラクタで必ず非 null（Global.SaveProfile へのフォールバックを削除）
            bool saved = profileSettingsVM.SaveProfile(profileName);

            if (saved)
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Editor_ProfileSaved", profileName);
                // 保存（Save）と適用（Apply）の違いは、画面を閉じるかどうかだけ。
                // どちらも親画面へ ProfileSaved を通知し、MainWindow.SyncProfileListAndControllers が、
                // プロファイル一覧を読み込み直したうえで、このプロファイルを使っているスロットにだけ再適用する。
                // 編集画面を開いたコントローラー（targetDevice）へ直接適用はしない（新規作成したプロファイルで
                // コントローラーが切り替わらないようにするため）。targetDevice はプレビュー・校正・ランブルテスト専用
                AppLogger.LogDebug($"[ProfileEditor] {(isApply ? "Apply" : "Save")}: profile '{profileName}' saved; re-applying to controller slots that use it (targetDevice={targetDevice})");
                ProfileSaved?.Invoke(this, profileName);
                return true;
            }
            else
            {
                AppLogger.LogUser(AppLogLevel.Error, "Log_Editor_ProfileSaveFailed", profileName);
                return false;
            }
        }

        private void SaveAndClose()
        {
            if (ExecuteSaveOrApply(isApply: false))
            {
                Close();
            }
        }
        private void ApplyEdit()
        {
            ExecuteSaveOrApply(isApply: true);
        }

        public void Close()
        {
            backend.StopRumble(profileSettingsVM.FuncDevNum);

            // 画面サイズ保持チェックが有効な場合のみレイアウト保存
            if (keepsize)
            {
                SaveSplitterAndColumnWidths();
            }

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void RumbleTestBtn_Click(object sender, RoutedEventArgs e)
        {
            // 振動させるのは実機（FuncDevNum＝targetDevice、実機なしならコントローラー0）。
            // モーターの左右反転は、編集中の設定（作業スロット deviceNum）の値を読む。判断は RumbleTestController が行い、
            // ここは押したボタンの文字だけを変える
            if (sender is not Button btn) return;
            bool heavyButton = btn.Name == "heavyRumbleTestBtn";
            var outcome = rumbleTest.Toggle(profileSettingsVM.Output, profileSettingsVM.FuncDevNum, heavyButton);
            if (outcome == RumbleTestOutcome.None) return;

            bool started = outcome == RumbleTestOutcome.Started;
            if (heavyButton)
                outputPanel.HeavyRumbleTestBtn.Content = started ? Properties.Resources.StopHText : Properties.Resources.TestHText;
            else
                outputPanel.LightRumbleTestBtn.Content = started ? Properties.Resources.StopLText : Properties.Resources.TestLText;
        }

        private void LaunchProgBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.AddExtension = true;
            dialog.DefaultExt = ".exe";
            dialog.Filter = "Program (*.exe)|*.exe";
            dialog.Title = "Select Program";

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (dialog.ShowDialog() == true)
            {
                profileSettingsVM.General.UpdateLaunchProgram(dialog.FileName);
            }
        }

        private void OutConTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ComboBox outConTypeCombo = outputPanel.OutConTypeCombo;
            if (outConTypeCombo == null)
                return;

            OutContType selected = OutContTypeInfo.FromProfileChoiceIndex(outConTypeCombo.SelectedIndex);

            if (profileSettingsVM != null)
            {
                profileSettingsVM.Output.ControllerTypeIndex = outConTypeCombo.SelectedIndex;
                profileSettingsVM.Output.TempControllerIndex = outConTypeCombo.SelectedIndex;
                profileSettingsVM.Output.TempConType = selected;
            }

            if (mappingListVM != null)
            {
                mappingListVM.UpdateMappingDevType(selected);
            }
        }
        private void NewAction()
        {
            if (leftPaneEditor != null) return;
            ProfileList profList = (Application.Current.MainWindow as MainWindow)!.ProfileListHolder;   // 画面が開いているあいだ、メイン画面は必ずある
            AssignmentEditor actEditor = editorWindowFactory.CreateAssignmentEditor(deviceNum, profList, targetDevice);
            OpenLeftPaneEditor(actEditor);
            actEditor.Cancel += (sender2, args) =>
            {
                CloseLeftPaneEditor();
            };
            actEditor.Saved += (sender2, actionName) =>
            {
                specialActionsVM.AddSavedAction(actionName);
                CloseLeftPaneEditor();
                // After adding a new special action, re-apply the current sort
                // so the list reflects the user's chosen sort column and order.
                SortSpecialActionsList(specialActionsVM.CurrentSortColumn, specialActionsVM.CurrentSortAscending);
                // Persistence and cache update deferred to Apply/Save to avoid
                // emitting removed-invalid logs at UI-time.
            };
        }

        private void Ds4LightbarColorBtn_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            DS4Color tempcolor = profileSettingsVM.Lightbar.MainColor;
            dialog.colorPicker.SelectedColor = MediaColors.ToMedia(tempcolor);
            profileSettingsVM.Lightbar.StartForcedColor(tempcolor);
            dialog.ColorChanged += (sender2, color) =>
            {
                profileSettingsVM.Lightbar.UpdateForcedColor(MediaColors.ToDS4(color));
            };
            dialog.ShowDialog();
            profileSettingsVM.Lightbar.EndForcedColor();
            profileSettingsVM.Lightbar.UpdateMainColor(MediaColors.ToDS4(dialog.colorPicker.SelectedColor.GetValueOrDefault()));
        }

        private void UseControllerReadoutCk_Click(object sender, RoutedEventArgs e)
        {
            // 判定は編集スロット（常に 8）ではなく実機の有無で行う（一覧経由では従来どおり開始しない）
            if (profileSettingsVM.General.UseControllerReadout && HasTargetDevice)
            {
                inputTimer?.Start();
            }
            else
            {
                inputTimer?.Stop();
            }
        }
    }
}

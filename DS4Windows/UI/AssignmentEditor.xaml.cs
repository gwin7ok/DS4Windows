/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for AssignmentEditor.xaml
    /// </summary>
    public partial class AssignmentEditor : UserControl
    {

        // 編集の状態と業務の操作は AssignmentEditorSession が持つ（V2）。画面は、表示の切り替えと、ダイアログの位置決めだけを行う
        private readonly AssignmentEditorSession session;
        private readonly ActionEditorViewModel specialActVM;
        private readonly bool controlMode;
        private readonly MacroViewModel macroActVM;
        private readonly LaunchProgramViewModel launchProgVM;
        private readonly LoadProfileViewModel loadProfileVM;
        private readonly PressKeyViewModel pressKeyVM;
        private readonly SpecialActionViewModel disconnectBtVM;
        private readonly CheckBatteryViewModel checkBatteryVM;
        private readonly MultiActButtonViewModel multiActButtonVM;
        private readonly SpecialActionViewModel calibrateGyroVM;
        private readonly FocusWindowViewModel focusWindowVM;

        public event EventHandler Cancel;

        /// <summary>Controls のモードか。</summary>
        public bool IsControlMode => controlMode;

        /// <summary>Controls のモードで「保存」が押され、入力が有効なときに発生する（受け手は、ProfileEditor の ShowControlAssignmentEditor）。</summary>
        public event EventHandler ControlSaved;

        public delegate void SaveHandler(object sender, string actionName);
        public event SaveHandler Saved;

        /// <summary>記録の画面を開くコマンドなど、編集の操作の窓口（XAML の Command が結ぶ）。</summary>
        public AssignmentEditorSession Session => session;

        private bool HasTargetDevice => session.HasTargetDevice;
        private int targetDevice => session.TargetDevice;

        public AssignmentEditor(AssignmentEditorSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            this.session = session;
            InitializeComponent();
            focusWindowTarget.Picker = session.WindowPicker;

            controlMode = session.ControlMode;
            specialActVM = session.ActionVM;
            macroActVM = session.MacroVM;
            launchProgVM = session.LaunchProgVM;
            loadProfileVM = session.LoadProfileVM;
            pressKeyVM = session.PressKeyVM;
            disconnectBtVM = session.DisconnectBtVM;
            checkBatteryVM = session.CheckBatteryVM;
            multiActButtonVM = session.MultiActVM;
            calibrateGyroVM = session.CalibrateGyroVM;
            focusWindowVM = session.FocusWindowVM;

            // Hide tab headers. Tab content will still be visible
            blankActTab.Visibility = Visibility.Collapsed;
            macroActTab.Visibility = Visibility.Collapsed;
            launchProgActTab.Visibility = Visibility.Collapsed;
            loadProfileTab.Visibility = Visibility.Collapsed;
            pressKetActTab.Visibility = Visibility.Collapsed;
            disconnectBTTab.Visibility = Visibility.Collapsed;
            checkBatteryTab.Visibility = Visibility.Collapsed;
            multiActTab.Visibility = Visibility.Collapsed;
            gyroCalibrateTab.Visibility = Visibility.Collapsed;
            focusWindowTab.Visibility = Visibility.Collapsed;
            defaultActTab.Visibility = Visibility.Collapsed;
            unboundActTab.Visibility = Visibility.Collapsed;

            // Phase10-Step3: 入力の制限は、種別の一覧の項目の表示・非表示だけで行う（検証・保存ボタンは、モードを区別しない）。
            // Controls のモード: 「種別を選んでください」（添字 0）を隠す（選べるのは、Default／Macro／Output Key/Mouse/Button／Unbound）。
            // SA のモード: Controls のモードだけの「Default」「Unbound」を隠す
            if (controlMode)
            {
                ((ComboBoxItem)actionTypeCombo.Items[0]).Visibility = Visibility.Collapsed;
            }
            else
            {
                defaultTypeItem.Visibility = Visibility.Collapsed;
                unboundTypeItem.Visibility = Visibility.Collapsed;
            }

            // 種別の一覧には、全種別を出し、このモードで現在保存できない種別は、選べなくする（Controls は、Step3 まで Key・Button・Macro だけ）
            for (int i = 0; i < actionTypeCombo.Items.Count; i++)
            {
                if (actionTypeCombo.Items[i] is ComboBoxItem typeItem && i < specialActVM.TypeCatalog.All.Count)
                {
                    typeItem.IsEnabled = specialActVM.IsTypeEnabled(i);
                }
            }

            actionTypeTabControl.DataContext = specialActVM;
            actionTypeCombo.DataContext = specialActVM;
            actionNameTxt.DataContext = specialActVM;
            triggersListView.DataContext = specialActVM;

            macroActTab.DataContext = macroActVM;
            launchProgActTab.DataContext = launchProgVM;
            loadProfileTab.DataContext = loadProfileVM;
            pressKetActTab.DataContext = pressKeyVM;
            disconnectBTTab.DataContext = disconnectBtVM;
            checkBatteryTab.DataContext = checkBatteryVM;
            multiActTab.DataContext = multiActButtonVM;
            gyroCalibrateTab.DataContext = calibrateGyroVM;
            focusWindowTab.DataContext = focusWindowVM;

            SetupLateEvents();
        }

        private void SetupLateEvents()
        {
            actionTypeCombo.SelectionChanged += ActionTypeCombo_SelectionChanged;
        }

        private void UnregisterDataContext()
        {
            actionTypeTabControl.DataContext = null;
            actionTypeCombo.DataContext = null;
            actionNameTxt.DataContext = null;
            triggersListView.DataContext = null;

            macroActTab.DataContext = null;
            launchProgActTab.DataContext = null;
            loadProfileTab.DataContext = null;
            pressKetActTab.DataContext = null;
            disconnectBTTab.DataContext = null;
            checkBatteryTab.DataContext = null;
            multiActTab.DataContext = null;
            gyroCalibrateTab.DataContext = null;
            focusWindowTab.DataContext = null;
        }

        private void ActionTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (specialActVM.ActionTypeIndex <= 0)
            {
                saveBtn.IsEnabled = false;
            }
            else
            {
                saveBtn.IsEnabled = true;
            }

            triggersListView.Visibility = Visibility.Visible;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            UnregisterDataContext();

            Cancel?.Invoke(this, EventArgs.Empty);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (controlMode)
            {
                // 入力が無効なときは、結びつきを外さず、エラーを表示したまま戻る
                if (!session.TrySaveControl()) return;

                UnregisterDataContext();
                ControlSaved?.Invoke(this, EventArgs.Empty);
                return;
            }

            // 入力が無効なときに先にデータの結びつきを外すと、エラーが表示される前に編集画面が空になる。無効なときは結びつきを保つ
            AssignmentEditorSession.SaveOutcome outcome = session.TrySaveSpecialAction(UnregisterDataContext, out string savedName);
            if (outcome == AssignmentEditorSession.SaveOutcome.ExistingName)
            {
                // 保存ボタンの近くへメッセージを出し、結びつきを保ったまま戻る（名前を直せるように）
                try
                {
                    ShowMessageWindowAt(Properties.Resources.ActionExists, saveBtn);
                }
                catch
                {
                    MessageBox.Show(Properties.Resources.ActionExists);
                }
            }
            else if (outcome == AssignmentEditorSession.SaveOutcome.Saved)
            {
                Saved?.Invoke(this, savedName);
            }
        }

        // Unload trigger toggle buttons and handlers removed: Key-type SpecialActions use single-trigger toggle mode.

        private void BatteryEmptyColorBtn_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            DS4Color tempcolor = checkBatteryVM.EmptyColor;
            dialog.colorPicker.SelectedColor = MediaColors.ToMedia(tempcolor);
            // Phase6-Step7b: 色のプレビューは実機（targetDevice）に対して行う。実機なしなら何もしない
            if (HasTargetDevice) checkBatteryVM.StartForcedColor(tempcolor, targetDevice);
            dialog.ColorChanged += (sender2, color) =>
            {
                if (HasTargetDevice) checkBatteryVM.UpdateForcedColor(MediaColors.ToDS4(color), targetDevice);
            };
            dialog.ShowDialog();
            if (HasTargetDevice) checkBatteryVM.EndForcedColor(targetDevice);
            checkBatteryVM.EmptyColor = MediaColors.ToDS4(dialog.colorPicker.SelectedColor.GetValueOrDefault());
        }

        private void BatteryFullColorBtn_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow dialog = new ColorPickerWindow();
            dialog.Owner = Application.Current.MainWindow;
            DS4Color tempcolor = checkBatteryVM.FullColor;
            dialog.colorPicker.SelectedColor = MediaColors.ToMedia(tempcolor);
            // Phase6-Step7b: 色のプレビューは実機（targetDevice）に対して行う。実機なしなら何もしない
            if (HasTargetDevice) checkBatteryVM.StartForcedColor(tempcolor, targetDevice);
            dialog.ColorChanged += (sender2, color) =>
            {
                if (HasTargetDevice) checkBatteryVM.UpdateForcedColor(MediaColors.ToDS4(color), targetDevice);
            };
            dialog.ShowDialog();
            if (HasTargetDevice) checkBatteryVM.EndForcedColor(targetDevice);
            checkBatteryVM.FullColor = MediaColors.ToDS4(dialog.colorPicker.SelectedColor.GetValueOrDefault());
        }

        private void PressKeySelectBtn_Click(object sender, RoutedEventArgs e)
        {
            // 出力ピッカーで Default／Unbound を選んだときは、種別の一覧の「Default」「Unbound」の項目へ切り替える（一覧と、選んだ内容を一致させる）
            int? typeIndex = session.PickOutput();
            if (typeIndex.HasValue) actionTypeCombo.SelectedIndex = typeIndex.Value;
        }

        // Show a transient message window positioned near the given anchor element.
        // This creates a small modal window with an OK button and positions it
        // slightly below the anchor (e.g., the Save button).
        private void ShowMessageWindowAt(string message, FrameworkElement anchor)
        {
            // Keep the existing custom window implementation as a fallback.
            Window owner = Application.Current?.MainWindow;

            Window msgWin = new Window()
            {
                Title = string.Empty,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.Manual,
                FontFamily = SystemFonts.MessageFontFamily,
                FontSize = SystemFonts.MessageFontSize
            };

            var panel = new StackPanel() { Margin = new Thickness(16) };
            var tb = new TextBlock()
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                MaxWidth = 420
            };
            var okBtn = new Button() { Content = "OK", IsDefault = true, HorizontalAlignment = HorizontalAlignment.Center, MinWidth = 80 };
            okBtn.Click += (s, e) => msgWin.Close();
            panel.Children.Add(tb);
            panel.Children.Add(okBtn);
            msgWin.Content = panel;

            // Compute screen position for anchor and horizontally center the dialog
            double centerX = 0, top = 0;
            try
            {
                if (anchor != null)
                {
                    var anchorPt = anchor.PointToScreen(new Point(0, 0));
                    var src = PresentationSource.FromVisual(this);
                    if (src != null && src.CompositionTarget != null)
                    {
                        var transform = src.CompositionTarget.TransformFromDevice;
                        var dpiPt = transform.Transform(anchorPt);
                        AppLogger.LogDebug($"AssignmentEditor: anchorPt(physical)={anchorPt} dpiPt(logical)={dpiPt} anchorSize=({anchor.ActualWidth}x{anchor.ActualHeight})");
                        centerX = dpiPt.X + anchor.ActualWidth / 2.0;
                        top = dpiPt.Y + anchor.ActualHeight + 8; // slightly below anchor
                    }
                    else
                    {
                        centerX = anchorPt.X + anchor.ActualWidth / 2.0;
                        top = anchorPt.Y + anchor.ActualHeight + 8;
                    }
                }
                else if (owner != null)
                {
                    centerX = owner.Left + 200;
                    top = owner.Top + 100;
                }
            }
            catch (Exception caught) { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ AppLogger.LogDebug($"[AssignmentEditor:332] ignored {caught.GetType().Name}: {caught.Message}"); }

            // Measure content to determine width, then position so dialog is centered
            panel.Measure(new Size(600, 400));
            double contentW = panel.DesiredSize.Width;
            double windowW = contentW + 32; // include margins/chrome approximation
            msgWin.Left = centerX - windowW / 2.0;
            msgWin.Top = top;

            msgWin.ShowDialog();
        }

        // Show a native MessageBox positioned by creating an invisible tiny owner
        // window located at the desired anchor point. MessageBox centers itself
        // over its owner, so placing the owner at the anchor makes the MessageBox
        // appear at that spot while preserving native look-and-feel.
        private void ShowPositionedMessageBox(string message, FrameworkElement anchor)
        {
            // Compute target point (screen coordinates in WPF units)
            double left = 0, top = 0;
            try
            {
                if (anchor != null)
                {
                    var anchorPt = anchor.PointToScreen(new Point(0, 0));
                    var src = PresentationSource.FromVisual(this);
                    if (src != null && src.CompositionTarget != null)
                    {
                        var transform = src.CompositionTarget.TransformFromDevice;
                        var dpiPt = transform.Transform(anchorPt);
                        AppLogger.LogDebug($"AssignmentEditor.ShowPositionedMessageBox: anchorPt(physical)={anchorPt} dpiPt(logical)={dpiPt} anchorSize=({anchor.ActualWidth}x{anchor.ActualHeight})");
                        left = dpiPt.X + anchor.ActualWidth / 2.0; // center anchor horizontally
                        top = dpiPt.Y + anchor.ActualHeight + 8; // slightly below anchor
                    }
                    else
                    {
                        left = anchorPt.X + anchor.ActualWidth / 2.0;
                        top = anchorPt.Y + anchor.ActualHeight + 8;
                    }
                }
                else
                {
                    var owner = Application.Current?.MainWindow;
                    if (owner != null)
                    {
                        left = owner.Left + 100;
                        top = owner.Top + 100;
                    }
                }
            }
            catch (Exception caught) { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ AppLogger.LogDebug($"[AssignmentEditor:382] ignored {caught.GetType().Name}: {caught.Message}"); }

            // Create tiny transparent owner window at the computed point
            var ownerWindow = new Window()
            {
                Width = 1,
                Height = 1,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            // Place owner so its center is at (left, top). Adjust to account for 1x1 size.
            ownerWindow.Left = left - 0.5;
            ownerWindow.Top = top - 0.5;

            try
            {
                ownerWindow.Show();
                // Use ownerWindow as owner for native MessageBox
                MessageBox.Show(ownerWindow, message);
            }
            finally
            {
                try { ownerWindow.Close(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            }
        }
    }
}

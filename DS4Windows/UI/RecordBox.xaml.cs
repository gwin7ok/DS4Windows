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
using System.Windows.Input;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for RecordBox.xaml
    /// </summary>
    public partial class RecordBox : UserControl
    {
        private RecordBoxViewModel recordBoxVM;
        // マウスの移動の記録（画面全体のマウス入力の監視と、記録のロジック）
        private readonly DS4Windows.UI.Services.MouseRecordingHost mouseHook;
        private DS4Windows.Processing.Macro.MacroPointerRecorder? pointerRecorder;
        private bool limitStopRequested;
        public RecordBoxViewModel RecordBoxVM { get => recordBoxVM; }

        private bool saved;
        public bool Saved { get => saved; }

        public event EventHandler? Save;
        public event EventHandler? Cancel;

        private ColorPickerWindow? colorDialog;
        private System.Timers.Timer ds4 = new System.Timers.Timer();

        // Program.rootHub／AppHost への後退を廃止し、必要なサービスを必須引数で受け取る。

        /// <param name="targetDevice">
        /// ライトバーのプレビューと記録中のタッチパッド Passthru で使う実機のスロット番号。-1 は実機なし。
        /// 渡し忘れをコンパイルで検出するため必須引数とする
        /// </param>
        public RecordBox(RecordBoxViewModel viewModel,
            bool showscan, bool showPlaybackOptions,
            DS4Windows.UI.Services.MouseRecordingHost mouseHook)
        {
            ArgumentNullException.ThrowIfNull(mouseHook);
            this.mouseHook = mouseHook;
            InitCommands();
            InitializeComponent();
            ArgumentNullException.ThrowIfNull(viewModel);
            recordBoxVM = viewModel;
            // Execute Mode と Scan Code は、共通部品 MacroPlaybackOptionsPanel（repeatable は PlaybackOptions.Repeatable に入る）
            playbackPanel.ShowScanCode = showscan;
            // SA から開くときは、Execute Mode／Scan Code を出さない（SA の Macro タブで設定する。記録専用の画面にする）
            playbackPanel.Visibility = showPlaybackOptions ? Visibility.Visible : Visibility.Collapsed;

            ds4.Enabled = false;
            ds4.Elapsed += Ds4_Tick;
            ds4.Interval = 10;
            DataContext = recordBoxVM;
            SetupLateEvents();
        }

        private void Ds4_Tick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            ds4.Stop();

            recordBoxVM.ProcessDS4Tick();

            if (recordBoxVM.Recording)
            {
                ds4.Start();
            }
        }

        private void SetupLateEvents()
        {
            macroListBox.SelectionChanged += MacroListBox_SelectionChanged;
            recordBoxVM.MacroSteps.CollectionChanged += MacroSteps_CollectionChanged;
            Save += RecordBox_Save;
            Cancel += RecordBox_Cancel;
            Unloaded += (sender, e) => StopMouseRecording();
        }

        private void RecordBox_Cancel(object? sender, EventArgs e)
        {
            recordBoxVM.RevertControlsSettings();
            ds4.Stop();
            ds4.Elapsed -= Ds4_Tick;
        }

        private void RecordBox_Save(object? sender, EventArgs e)
        {
            recordBoxVM.RevertControlsSettings();
            ds4.Stop();
            ds4.Elapsed -= Ds4_Tick;
        }

        private void MacroListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!recordBoxVM.Recording)
            {
                recordBtn.Content = "Record";
            }
        }

        private void SaveMacro()
        {
            if (recordBoxVM.EditMacroIndex > -1)
            {
                UpdateDataRevertTemplate();
            }

            saved = true;
            recordBoxVM.ExportMacro();
            DataContext = null;
            Save?.Invoke(this, EventArgs.Empty);
        }

        private void CancelMacro()
        {
            DataContext = null;
            Cancel?.Invoke(this, EventArgs.Empty);
        }

        private void ClearSteps()
        {
            if (!recordBoxVM.Recording)
            {
                recordBoxVM.MacroStepIndex = -1;
                recordBoxVM.MacroSteps.Clear();
            }
        }

        private void LoadPreset()
        {
            // Alt-Tab 用のプリセットの削除で、メニューの項目が 1 つになったため、メニューをやめて、保存のボタンと同じく、ファイルの選択を直接表示する
            // Reset selected index of macro list before removing item source
            macroListBox.SelectedIndex = -1;

            macroListBox.ItemsSource = null;
            recordBoxVM.PickAndLoadPreset();

            macroListBox.ItemsSource = recordBoxVM.MacroSteps;
        }

        private void SavePreset()
        {
            // Reset selected index of macro list before removing item source
            macroListBox.SelectedIndex = -1;

            recordBoxVM.PickAndSavePreset();
        }

        private void PointerModeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
        }

        private void PointerMoveEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && recordBoxVM.EditMacroIndex > -1)
            {
                UpdateDataRevertTemplate();
                e.Handled = true;
            }
        }

        private void StopMouseRecording()
        {
            mouseHook.PointerMoved -= MouseHook_PointerMoved;
            mouseHook.ButtonChanged -= MouseHook_ButtonChanged;
            mouseHook.Stop();
            pointerRecorder = null;
        }

        private void AddPointerMoveStep(int macroValue)
        {
            var move = DS4Windows.Processing.Macro.MacroPointerMove.TryFromMacroValue(macroValue, out DS4Windows.Processing.Macro.MacroPointerMove decoded) ? decoded.DisplayName : "Move";
            recordBoxVM.AddMacroStep(new DS4Windows.Processing.Macro.MacroStep(macroValue, move,
                DS4Windows.Processing.Macro.MacroStep.StepType.ActDown, DS4Windows.Processing.Macro.MacroStep.StepOutput.PointerMove));
        }

        // ---- 行の選択とドラッグ＆ドロップによる並べ替え ----
        private const string DragFormat = "DS4Windows.MacroStepIndexes";
        private Point? dragStartPoint;
        private ListBoxItem? pendingCollapseItem;

        private ListBoxItem? ItemUnder(DependencyObject? source) => source == null ? null : ItemsControl.ContainerFromElement(macroListBox, source) as ListBoxItem;

        private void MacroListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = !recordBoxVM.Recording && e.Data.GetDataPresent(DragFormat) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }
    }
}
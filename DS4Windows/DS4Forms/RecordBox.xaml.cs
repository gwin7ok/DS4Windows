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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NonFormTimer = System.Timers.Timer;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;
using DS4WinWPF.DS4Forms.ViewModels;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Interaction logic for RecordBox.xaml
    /// </summary>
    public partial class RecordBox : UserControl
    {
        private RecordBoxViewModel recordBoxVM;
        // Phase12-Step3 3e（R-3）: マウスの移動の記録（画面全体のマウス入力の監視と、記録のロジック）
        private readonly DS4Windows.Services.IWindowSystem windowSystem;
        private readonly DS4Windows.Services.IMouseInputHook mouseHook;
        private DS4Windows.MacroPointerRecorder pointerRecorder;
        private bool limitStopRequested;
        public RecordBoxViewModel RecordBoxVM { get => recordBoxVM; }

        private bool saved;
        public bool Saved { get => saved; }

        public event EventHandler Save;
        public event EventHandler Cancel;

        private ColorPickerWindow colorDialog;
        private NonFormTimer ds4 = new NonFormTimer();

        // Phase6-Step10-2-4b: Program.rootHub／AppHost への後退を廃止し、必要なサービスを必須引数で受け取る。
        private readonly DS4Windows.ControlService controlService;
        private readonly DS4Windows.DI.IPathService pathService;

        /// <param name="targetDevice">
        /// Phase6-Step7b: ライトバーのプレビューと記録中のタッチパッド Passthru で使う実機のスロット番号。-1 は実機なし。
        /// 渡し忘れをコンパイルで検出するため必須引数とする（Phase6-Step7b-Plan.md 決定2）
        /// </param>
        public RecordBox(int deviceNum, DS4Windows.DS4ControlSettings controlSettings, int targetDevice,
            bool showscan, bool repeatable, bool showPlaybackOptions,
            DS4Windows.ControlService controlService, DS4Windows.DI.IPathService pathService,
            DS4Windows.DI.IViewModelFactory viewModelFactory,
            DS4Windows.Services.IWindowSystem windowSystem, DS4Windows.Services.IMouseInputHook mouseHook)
        {
            this.windowSystem = windowSystem ?? throw new ArgumentNullException(nameof(windowSystem));
            this.mouseHook = mouseHook ?? throw new ArgumentNullException(nameof(mouseHook));
            this.controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
            this.pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            if (viewModelFactory == null) throw new ArgumentNullException(nameof(viewModelFactory));
            InitializeComponent();
            recordBoxVM = viewModelFactory.CreateRecordBoxViewModel(deviceNum, controlSettings, repeatable, targetDevice);
            mouseButtonsPanel.Visibility = Visibility.Hidden;
            extraConPanel.Visibility = Visibility.Hidden;
            // Phase10-Step1 W2: Execute Mode と Scan Code は、共通部品 MacroPlaybackOptionsPanel（repeatable は PlaybackOptions.Repeatable に入る）
            playbackPanel.ShowScanCode = showscan;
            // Phase10-Step1 W3: SA から開くときは、Execute Mode／Scan Code を出さない（SA の Macro タブで設定する。記録専用の画面にする）
            playbackPanel.Visibility = showPlaybackOptions ? Visibility.Visible : Visibility.Collapsed;

            ds4.Enabled = false;
            ds4.Elapsed += Ds4_Tick;
            ds4.Interval = 10;
            DataContext = recordBoxVM;
            SetupLateEvents();
        }

        private void Ds4_Tick(object sender, System.Timers.ElapsedEventArgs e)
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

        private void RecordBox_Cancel(object sender, EventArgs e)
        {
            recordBoxVM.RevertControlsSettings();
            ds4.Stop();
            ds4.Elapsed -= Ds4_Tick;
        }

        private void RecordBox_Save(object sender, EventArgs e)
        {
            recordBoxVM.RevertControlsSettings();
            ds4.Stop();
            ds4.Elapsed -= Ds4_Tick;
        }

        private void MacroSteps_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    int count = recordBoxVM.MacroSteps.Count;
                    if (count > 0)
                    {
                        macroListBox.ScrollIntoView(recordBoxVM.MacroSteps[count - 1]);
                    }
                }));
            }
        }

        private void MacroListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Phase12-Step3 3d: 編集部品の中の ComboBox（マウス移動のモード）の SelectionChanged も、ここまで伝わってくる。
            // それで編集部品を確定・差し替えると、ComboBox の操作の最中に木が壊れてクラッシュするため、ListBox 自身の選択の変更だけを扱う
            if (!ReferenceEquals(e.OriginalSource, macroListBox)) return;

            // 挿入位置の基準は、選択された行のうち、いちばん上の行（選択がなければ -1＝いちばん下に足す）
            recordBoxVM.MacroStepIndex = RecordBoxViewModel.FirstIndexOf(
                macroListBox.SelectedItems.Cast<MacroStepItem>().Select(item => recordBoxVM.MacroSteps.IndexOf(item)));

            if (!recordBoxVM.Recording)
            {
                if (recordBoxVM.MacroStepIndex >= 0)
                {
                    MacroStepItem item = recordBoxVM.MacroSteps[recordBoxVM.MacroStepIndex];
                    recordBtn.Content = $"Record Before {item.Step.Name}";
                }
                else
                {
                    recordBtn.Content = "Record";
                }

                if (recordBoxVM.EditMacroIndex > -1)
                {
                    UpdateDataRevertTemplate();
                }
            }
        }

        private void MacroListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!recordBoxVM.Recording)
            {
                recordBtn.Content = "Record";
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
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

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DataContext = null;
            Cancel?.Invoke(this, EventArgs.Empty);
        }

        private void RecordBtn_Click(object sender, RoutedEventArgs e)
        {
            bool recording = recordBoxVM.Recording = !recordBoxVM.Recording;
            if (recording)
            {
                controlService.recordingMacro = true;
                recordBtn.Content = "Stop";
                if (recordBoxVM.MacroStepIndex == -1)
                {
                    // Don't clear macro steps in RECORD button because nowadays there is a separate CLEAR button. RECORD btn without a selection appends new steps to existing macro sequence
                    //recordBoxVM.MacroSteps.Clear();
                }
                else
                {
                    recordBoxVM.AppendIndex = recordBoxVM.MacroStepIndex;
                }

                // 記録中は、4th/5th Mouse Button と Add Rumble/Change Lightbar Color の 4 つを同じ条件で表示する。
                // 2026-09-26（ユーザー決定）: 以前は Add Rumble/Change Lightbar Color だけ Record Delays にチェックがある
                // ときに限って表示していたが、その制限を撤廃した。Record Delays がオフの場合、開始と終了のステップの間に
                // Wait が入らないため、必要に応じて記録後に Insert Wait やダブルクリックで待ち時間を調整する
                mouseButtonsPanel.Visibility = Visibility.Visible;
                extraConPanel.Visibility = Visibility.Visible;

                ds4.Start();
                Enable_Controls(false);
                recordBoxVM.Sw.Restart();
                this.Focus();
                StartMouseRecording();
            }
            else
            {
                controlService.recordingMacro = false;
                recordBoxVM.AppendIndex = -1;
                StopMouseRecording();
                ds4.Stop();
                recordBtn.Content = "Record";
                mouseButtonsPanel.Visibility = Visibility.Hidden;
                extraConPanel.Visibility = Visibility.Hidden;
                recordBoxVM.Sw.Stop();

                if (recordBoxVM.Toggle4thMouse)
                {
                    FourMouseBtnAction();
                }
                if (recordBoxVM.Toggle5thMouse)
                {
                    FiveMouseBtnAction();
                }
                if (recordBoxVM.ToggleLightbar)
                {
                    ChangeLightbarAction();
                }
                if (recordBoxVM.ToggleRumble)
                {
                    ChangeRumbleAction();
                }

                Enable_Controls(true);
            }

            recordBoxVM.EditMacroIndex = -1;
            recordBoxVM.ToggleLightbar = false;
            recordBoxVM.ToggleRumble = false;
            changeLightBtn.Content = "Change Lightbar Color";
            addRumbleBtn.Content = "Add Rumble";
            macroListBox.UnselectAll();
            recordBoxVM.MacroStepIndex = -1;
        }

        private void ClearStepsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!recordBoxVM.Recording)
            {
                recordBoxVM.MacroStepIndex = -1;
                recordBoxVM.MacroSteps.Clear();
            }
        }

        private void Enable_Controls(bool on)
        {
            macroListBox.IsEnabled = on;
            recordDelaysCk.IsEnabled = on;
            saveBtn.IsEnabled = on;
            cancelBtn.IsEnabled = on;
            loadPresetBtn.IsEnabled = on;
            savePresetBtn.IsEnabled = on;
            playbackPanel.AllowModeEdit = on;
            clearStepsBtn.IsEnabled = on;
            addWaitTimeBtn.IsEnabled = on;
            addMouseMoveBtn.IsEnabled = on;
            recordMouseMoveCk.IsEnabled = on;
            pointerRecordModeCb.IsEnabled = on;
        }

        private void ChangeLightbarAction()
        {
            bool light = recordBoxVM.ToggleLightbar = !recordBoxVM.ToggleLightbar;
            if (light)
            {
                changeLightBtn.Content = "Reset Lightbar Color";
                DS4Windows.MacroStep step = new DS4Windows.MacroStep(1255255255, $"Lightbar Color: 255,255,255",
                            DS4Windows.MacroStep.StepType.ActDown, DS4Windows.MacroStep.StepOutput.Lightbar);
                recordBoxVM.AddMacroStep(step);
            }
            else
            {
                changeLightBtn.Content = "Change Lightbar Color";
                DS4Windows.MacroStep step = new DS4Windows.MacroStep(1000000000, $"Reset Lightbar",
                            DS4Windows.MacroStep.StepType.ActUp, DS4Windows.MacroStep.StepOutput.Lightbar);
                recordBoxVM.AddMacroStep(step);
            }
        }

        private void ChangeLightBtn_Click(object sender, RoutedEventArgs e)
        {
            ChangeLightbarAction();
        }

        private void ChangeRumbleAction()
        {
            bool rumble = recordBoxVM.ToggleRumble = !recordBoxVM.ToggleRumble;
            if (rumble)
            {
                addRumbleBtn.Content = "Stop Rumble";
                DS4Windows.MacroStep step = new DS4Windows.MacroStep(1255255, $"Rumble 255,255",
                            DS4Windows.MacroStep.StepType.ActDown, DS4Windows.MacroStep.StepOutput.Rumble);
                recordBoxVM.AddMacroStep(step);
            }
            else
            {
                addRumbleBtn.Content = "Add Rumble";
                DS4Windows.MacroStep step = new DS4Windows.MacroStep(1000000, $"Stop Rumble",
                            DS4Windows.MacroStep.StepType.ActUp, DS4Windows.MacroStep.StepOutput.Rumble);
                recordBoxVM.AddMacroStep(step);
            }
        }

        private void AddRumbleBtn_Click(object sender, RoutedEventArgs e)
        {
            ChangeRumbleAction();
        }

        private void LoadPresetBtn_Click(object sender, RoutedEventArgs e)
        {
            // Phase8-Step7-3a: Alt-Tab 用のプリセットの削除で、メニューの項目が 1 つになったため、メニューをやめて、保存のボタンと同じく、ファイルの選択を直接表示する
            // Reset selected index of macro list before removing item source
            macroListBox.SelectedIndex = -1;

            macroListBox.ItemsSource = null;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text Documents (*.txt)|*.txt";
            dialog.Title = "Select Preset File";
            dialog.InitialDirectory = $"{pathService.AppDataPath}\\Macros";
            if (dialog.ShowDialog() == true)
            {
                recordBoxVM.MacroSteps.Clear();
                recordBoxVM.LoadPresetFromFile(dialog.FileName);
            }

            macroListBox.ItemsSource = recordBoxVM.MacroSteps;
        }

        private void SavePresetBtn_Click(object sender, RoutedEventArgs e)
        {
            // Reset selected index of macro list before removing item source
            macroListBox.SelectedIndex = -1;

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text Documents (*.txt)|*.txt";
            dialog.Title = "Select Export File";
            dialog.InitialDirectory = $"{pathService.AppDataPath}\\Macros";
            if (dialog.ShowDialog() == true)
            {
                //recordBoxVM.MacroSteps.Clear();
                recordBoxVM.SavePreset(dialog.FileName);
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (recordBoxVM.Recording)
            {
                Key tempKey = e.SystemKey == Key.None ? e.Key : e.SystemKey;
                int value = KeyInterop.VirtualKeyFromKey(tempKey);
                recordBoxVM.KeysdownMap.TryGetValue(value, out bool isdown);
                if (!isdown)
                {
                    DS4Windows.MacroStep step = new DS4Windows.MacroStep(value, tempKey.ToString(),
                            DS4Windows.MacroStep.StepType.ActDown, DS4Windows.MacroStep.StepOutput.Key);
                    recordBoxVM.AddMacroStep(step);
                    recordBoxVM.KeysdownMap.Add(value, true);
                }

                e.Handled = true;
                //Console.WriteLine(e.Key);
                //Console.WriteLine(e.SystemKey);
            }
            else if (e.Key == Key.Delete && recordBoxVM.EditMacroIndex < 0 && macroListBox.SelectedItems.Count > 0)
            {
                // 選択された行（Ctrl＋クリック・Shift＋クリックで選んだ複数の行を含む）をすべて消す
                List<int> selected = macroListBox.SelectedItems.Cast<MacroStepItem>().Select(item => recordBoxVM.MacroSteps.IndexOf(item)).ToList();
                macroListBox.UnselectAll();
                recordBoxVM.RemoveSteps(selected);
                e.Handled = true;
            }
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (recordBoxVM.Recording)
            {
                Key tempKey = e.SystemKey == Key.None ? e.Key : e.SystemKey;
                int value = KeyInterop.VirtualKeyFromKey(tempKey);
                recordBoxVM.KeysdownMap.TryGetValue(value, out bool isdown);
                if (isdown)
                {
                    DS4Windows.MacroStep step = new DS4Windows.MacroStep(value, tempKey.ToString(),
                            DS4Windows.MacroStep.StepType.ActUp, DS4Windows.MacroStep.StepOutput.Key);
                    recordBoxVM.AddMacroStep(step);
                    recordBoxVM.KeysdownMap.Remove(value);
                }
                else if (RecordBoxViewModel.KeydownOverrides.Contains(value))
                {
                    DS4Windows.MacroStep step = new DS4Windows.MacroStep(value, tempKey.ToString(),
                            DS4Windows.MacroStep.StepType.ActDown, DS4Windows.MacroStep.StepOutput.Key);
                    recordBoxVM.AddMacroStep(step, ignoreDelay: true);

                    step = new DS4Windows.MacroStep(value, tempKey.ToString(),
                            DS4Windows.MacroStep.StepType.ActUp, DS4Windows.MacroStep.StepOutput.Key);
                    recordBoxVM.AddMacroStep(step, ignoreDelay: true);
                }

                e.Handled = true;
                //Console.WriteLine(e.Key);
                //Console.WriteLine(e.SystemKey);
            }
        }

        private void MacroListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (recordBoxVM.MacroStepIndex >= 0)
            {
                MacroStepItem item = recordBoxVM.MacroSteps[recordBoxVM.MacroStepIndex];
                if (item.Step.ActType == DS4Windows.MacroStep.StepType.Wait)
                {
                    ListBoxItem lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.MacroStepIndex)
                        as ListBoxItem;
                    lbitem.ContentTemplate = this.FindResource("EditTemplate") as DataTemplate;
                    recordBoxVM.EditMacroIndex = recordBoxVM.MacroStepIndex;
                }
                else if (item.Step.OutputType == DS4Windows.MacroStep.StepOutput.PointerMove)
                {
                    // Phase12-Step3 3d（R-3）: マウス移動の座標の編集
                    ListBoxItem lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.MacroStepIndex)
                        as ListBoxItem;
                    lbitem.ContentTemplate = this.FindResource("EditPointerMoveTemplate") as DataTemplate;
                    recordBoxVM.EditMacroIndex = recordBoxVM.MacroStepIndex;
                }
                else if (item.Step.OutputType == DS4Windows.MacroStep.StepOutput.Rumble &&
                    item.Step.ActType == DS4Windows.MacroStep.StepType.ActDown)
                {
                    ListBoxItem lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.MacroStepIndex)
                        as ListBoxItem;
                    lbitem.ContentTemplate = this.FindResource("EditRumbleTemplate") as DataTemplate;
                    recordBoxVM.EditMacroIndex = recordBoxVM.MacroStepIndex;
                }
                else if (item.Step.OutputType == DS4Windows.MacroStep.StepOutput.Lightbar &&
                    item.Step.ActType == DS4Windows.MacroStep.StepType.ActDown)
                {
                    colorDialog = new ColorPickerWindow();
                    colorDialog.Owner = Application.Current.MainWindow;
                    Color tempcolor = item.LightbarColorValue();
                    colorDialog.colorPicker.SelectedColor = tempcolor;
                    recordBoxVM.StartForcedColor(tempcolor);
                    colorDialog.ColorChanged += (sender2, color) =>
                    {
                        recordBoxVM.UpdateForcedColor(color);
                    };
                    colorDialog.ShowDialog();
                    recordBoxVM.EndForcedColor();
                    item.UpdateLightbarValue(colorDialog.colorPicker.SelectedColor.GetValueOrDefault());

                    FocusNavigationDirection focusDirection = FocusNavigationDirection.Next;
                    TraversalRequest request = new TraversalRequest(focusDirection);
                    UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
                    elementWithFocus?.MoveFocus(request);
                }
            }
        }

        private void RevertListItemTemplate()
        {
            if (recordBoxVM.EditMacroIndex >= 0)
            {
                ListBoxItem lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.EditMacroIndex)
                        as ListBoxItem;
                lbitem.ContentTemplate = this.FindResource("DisplayTemplate") as DataTemplate;
                recordBoxVM.EditMacroIndex = -1;
            }
        }

        private void UpdateDataRevertTemplate()
        {
            ListBoxItem lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.EditMacroIndex)
                        as ListBoxItem;
            ContentPresenter contentPresenter = UtilMethods.FindVisualChild<ContentPresenter>(lbitem);
            DataTemplate oldDataTemplate = contentPresenter.ContentTemplate;

            MacroStepItem item = recordBoxVM.MacroSteps[recordBoxVM.EditMacroIndex];
            if (item.Step.ActType == DS4Windows.MacroStep.StepType.Wait)
            {
                IntegerUpDown integerUpDown = oldDataTemplate.FindName("waitIUD", contentPresenter) as IntegerUpDown;
                if (integerUpDown != null)
                {
                    BindingExpression bindExp = integerUpDown.GetBindingExpression(IntegerUpDown.ValueProperty);
                    bindExp.UpdateSource();
                }
            }
            else if (item.Step.OutputType == DS4Windows.MacroStep.StepOutput.PointerMove)
            {
                // モードを先に確定する（座標は、そのモードの範囲へ丸められる）
                ComboBox modeBox = oldDataTemplate.FindName("pointerModeCb", contentPresenter) as ComboBox;
                IntegerUpDown pointerX = oldDataTemplate.FindName("pointerXUD", contentPresenter) as IntegerUpDown;
                IntegerUpDown pointerY = oldDataTemplate.FindName("pointerYUD", contentPresenter) as IntegerUpDown;
                modeBox?.GetBindingExpression(ComboBox.SelectedIndexProperty)?.UpdateSource();
                pointerX?.GetBindingExpression(IntegerUpDown.ValueProperty)?.UpdateSource();
                pointerY?.GetBindingExpression(IntegerUpDown.ValueProperty)?.UpdateSource();
            }
            else if (item.Step.OutputType == DS4Windows.MacroStep.StepOutput.Rumble)
            {
                IntegerUpDown heavyRumble = oldDataTemplate.FindName("heavyRumbleUD", contentPresenter) as IntegerUpDown;
                IntegerUpDown lightRumble = oldDataTemplate.FindName("lightRumbleUD", contentPresenter) as IntegerUpDown;
                if (heavyRumble != null && lightRumble != null)
                {
                    BindingExpression bindExp = heavyRumble.GetBindingExpression(IntegerUpDown.ValueProperty);
                    bindExp.UpdateSource();

                    bindExp = lightRumble.GetBindingExpression(IntegerUpDown.ValueProperty);
                    bindExp.UpdateSource();
                }
            }

            lbitem.ContentTemplate = this.FindResource("DisplayTemplate") as DataTemplate;
            recordBoxVM.EditMacroIndex = -1;
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

        private void WaitIUD_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                IntegerUpDown integerUpDown = sender as IntegerUpDown;
                BindingExpression bindExp = integerUpDown.GetBindingExpression(IntegerUpDown.ValueProperty);
                bindExp.UpdateSource();

                ListBoxItem lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.EditMacroIndex)
                        as ListBoxItem;
                lbitem.ContentTemplate = this.FindResource("DisplayTemplate") as DataTemplate;
                recordBoxVM.EditMacroIndex = -1;
            }
        }

        private void FourMouseBtnAction()
        {
            int value = 259;
            recordBoxVM.KeysdownMap.TryGetValue(value, out bool isdown);
            if (!isdown)
            {
                DS4Windows.MacroStep step = new DS4Windows.MacroStep(value, DS4Windows.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.MacroStep.StepType.ActDown, DS4Windows.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Add(value, true);
            }
            else
            {
                DS4Windows.MacroStep step = new DS4Windows.MacroStep(value, DS4Windows.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.MacroStep.StepType.ActUp, DS4Windows.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Remove(value);
            }
        }

        private void FourMouseBtn_Click(object sender, RoutedEventArgs e)
        {
            FourMouseBtnAction();
        }

        private void FiveMouseBtnAction()
        {
            int value = 260;
            recordBoxVM.KeysdownMap.TryGetValue(value, out bool isdown);
            if (!isdown)
            {
                DS4Windows.MacroStep step = new DS4Windows.MacroStep(value, DS4Windows.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.MacroStep.StepType.ActDown, DS4Windows.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Add(value, true);
            }
            else
            {
                DS4Windows.MacroStep step = new DS4Windows.MacroStep(value, DS4Windows.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.MacroStep.StepType.ActUp, DS4Windows.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Remove(value);
            }
        }

        private void FiveMouseBtn_Click(object sender, RoutedEventArgs e)
        {
            FiveMouseBtnAction();
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (recordBoxVM.Recording)
            {
                int value;
                switch (e.ChangedButton)
                {
                    case MouseButton.Left: value = 256; break;
                    case MouseButton.Right: value = 257; break;
                    case MouseButton.Middle: value = 258; break;
                    case MouseButton.XButton1: value = 259; break;
                    case MouseButton.XButton2: value = 260; break;
                    default: value = 0; break;
                }

                DS4Windows.MacroStep step = new DS4Windows.MacroStep(value, DS4Windows.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.MacroStep.StepType.ActDown, DS4Windows.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Add(value, true);
                e.Handled = true;
            }
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (recordBoxVM.Recording)
            {
                int value;
                switch (e.ChangedButton)
                {
                    case MouseButton.Left: value = 256; break;
                    case MouseButton.Right: value = 257; break;
                    case MouseButton.Middle: value = 258; break;
                    case MouseButton.XButton1: value = 259; break;
                    case MouseButton.XButton2: value = 260; break;
                    default: value = 0; break;
                }

                DS4Windows.MacroStep step = new DS4Windows.MacroStep(value, DS4Windows.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.MacroStep.StepType.ActUp, DS4Windows.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Remove(value);
                e.Handled = true;
            }
        }

        // Phase12-Step3 3d（R-3）: マウス移動のステップを、選択した位置の前（選択がなければ末尾）へ入れ、選択する。座標は、ダブルクリックで直す
        private void StartMouseRecording()
        {
            if (!recordBoxVM.RecordMouseMovement) return;

            limitStopRequested = false;
            pointerRecorder = new DS4Windows.MacroPointerRecorder((DS4Windows.MacroPointerMoveMode)recordBoxVM.PointerRecordModeIndex,
                windowSystem, Environment.ProcessId);
            mouseHook.PointerMoved += MouseHook_PointerMoved;
            mouseHook.ButtonChanged += MouseHook_ButtonChanged;
            if (!mouseHook.Start())
            {
                StopMouseRecording();
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
            var move = DS4Windows.MacroPointerMove.TryFromMacroValue(macroValue, out DS4Windows.MacroPointerMove decoded) ? decoded.DisplayName : "Move";
            recordBoxVM.AddMacroStep(new DS4Windows.MacroStep(macroValue, move,
                DS4Windows.MacroStep.StepType.ActDown, DS4Windows.MacroStep.StepOutput.PointerMove));
        }

        private void MouseHook_PointerMoved(int x, int y)
        {
            if (!recordBoxVM.Recording || pointerRecorder == null) return;

            if (pointerRecorder.TryRecordMove(x, y, Environment.TickCount64, out int macroValue))
            {
                AddPointerMoveStep(macroValue);
                StopWhenLimitReached();
            }
        }

        private void MouseHook_ButtonChanged(int value, bool isDown, int x, int y)
        {
            if (!recordBoxVM.Recording || pointerRecorder == null) return;
            if (!pointerRecorder.TryBeginButton(x, y, Environment.TickCount64, out bool hasMove, out int moveValue)) return;

            if (hasMove)
            {
                AddPointerMoveStep(moveValue);
            }

            recordBoxVM.AddMacroStep(new DS4Windows.MacroStep(value, DS4Windows.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                isDown ? DS4Windows.MacroStep.StepType.ActDown : DS4Windows.MacroStep.StepType.ActUp, DS4Windows.MacroStep.StepOutput.Button));
            if (isDown)
            {
                recordBoxVM.KeysdownMap[value] = true;
            }
            else
            {
                recordBoxVM.KeysdownMap.Remove(value);
            }

            StopWhenLimitReached();
        }

        // 移動ステップが上限に達したら、記録を止めて知らせる（フックのコールバックの中では止めず、あとで UI のスレッドで止める）
        private void StopWhenLimitReached()
        {
            if (pointerRecorder == null || !pointerRecorder.LimitReached || limitStopRequested) return;

            limitStopRequested = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!recordBoxVM.Recording) return;

                RecordBtn_Click(this, new RoutedEventArgs());
                System.Windows.MessageBox.Show(string.Format(DS4WinWPF.Translations.Strings.RecordBox_MoveRecordLimitReached, DS4Windows.MacroPointerRecorder.MaxMoveSteps));
            }));
        }

        private void AddMouseMoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (recordBoxVM.Recording) return;

            if (recordBoxVM.EditMacroIndex > -1)
            {
                UpdateDataRevertTemplate();
            }

            int index = recordBoxVM.AddPointerMoveStep(recordBoxVM.MacroStepIndex);
            macroListBox.SelectedIndex = index;
            macroListBox.ScrollIntoView(recordBoxVM.MacroSteps[index]);
        }

        private void AddWaitTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (recordBoxVM.Recording) return;

            // 行を選択していれば、いちばん上の選択行の前へ。選択がなければ、いちばん下へ足す
            int index = recordBoxVM.MacroStepIndex >= 0 ? recordBoxVM.MacroStepIndex : recordBoxVM.MacroSteps.Count;
            DS4Windows.MacroStep step = new DS4Windows.MacroStep(400, "Wait 100ms",
                        DS4Windows.MacroStep.StepType.Wait, DS4Windows.MacroStep.StepOutput.None);
            recordBoxVM.InsertMacroStep(index, step);
        }

        // ---- 行の選択とドラッグ＆ドロップによる並べ替え ----
        private const string DragFormat = "DS4Windows.MacroStepIndexes";
        private Point? dragStartPoint;
        private ListBoxItem pendingCollapseItem;

        private ListBoxItem ItemUnder(DependencyObject source) => ItemsControl.ContainerFromElement(macroListBox, source) as ListBoxItem;

        private static bool IsOnScrollBar(DependencyObject source)
        {
            for (DependencyObject current = source; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is System.Windows.Controls.Primitives.ScrollBar) return true;
            }

            return false;
        }

        private void MacroListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = null;
            pendingCollapseItem = null;
            if (recordBoxVM.Recording || IsOnScrollBar(e.OriginalSource as DependencyObject)) return;

            ListBoxItem item = ItemUnder(e.OriginalSource as DependencyObject);
            if (item == null)
            {
                // コマンドのない行（空白）のクリック: 選択を外す（Record／Insert は、いちばん下に足す）
                if (recordBoxVM.EditMacroIndex > -1) UpdateDataRevertTemplate();
                macroListBox.UnselectAll();
                macroListBox.Focus();
                return;
            }

            dragStartPoint = e.GetPosition(macroListBox);
            bool plain = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.None;
            if (plain && item.IsSelected && macroListBox.SelectedItems.Count > 1)
            {
                // 複数選択の行を押した: そのままドラッグできるよう、選択をまだ 1 行にしない（ドラッグせずに離したら、その 1 行だけにする）
                pendingCollapseItem = item;
                macroListBox.Focus();
                e.Handled = true;
            }
        }

        private void MacroListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || dragStartPoint == null || recordBoxVM.Recording || recordBoxVM.EditMacroIndex > -1) return;

            Vector moved = e.GetPosition(macroListBox) - dragStartPoint.Value;
            if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            dragStartPoint = null;
            pendingCollapseItem = null;
            int[] indexes = macroListBox.SelectedItems.Cast<MacroStepItem>().Select(item => recordBoxVM.MacroSteps.IndexOf(item))
                .Where(i => i >= 0).OrderBy(i => i).ToArray();
            if (indexes.Length == 0) return;

            DragDrop.DoDragDrop(macroListBox, new DataObject(DragFormat, indexes), DragDropEffects.Move);
        }

        private void MacroListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = null;
            if (pendingCollapseItem != null)
            {
                // ドラッグせずに離した: 押した行だけの選択にする
                ListBoxItem item = pendingCollapseItem;
                pendingCollapseItem = null;
                macroListBox.SelectedItem = item.DataContext;
            }
        }

        // ドロップ先: 行の上半分なら、その行の前。下半分なら、その行の次の前。行のない所なら、いちばん下
        private int DropTargetIndex(DragEventArgs e)
        {
            ListBoxItem item = ItemUnder(e.OriginalSource as DependencyObject);
            if (item == null) return recordBoxVM.MacroSteps.Count;

            int index = macroListBox.ItemContainerGenerator.IndexFromContainer(item);
            if (index < 0) return recordBoxVM.MacroSteps.Count;

            return e.GetPosition(item).Y > item.ActualHeight / 2 ? index + 1 : index;
        }

        private void MacroListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = !recordBoxVM.Recording && e.Data.GetDataPresent(DragFormat) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void MacroListBox_Drop(object sender, DragEventArgs e)
        {
            if (recordBoxVM.Recording || !e.Data.GetDataPresent(DragFormat)) return;

            if (recordBoxVM.EditMacroIndex > -1) UpdateDataRevertTemplate();
            int[] sources = (int[])e.Data.GetData(DragFormat);
            int[] moved = recordBoxVM.MoveSteps(sources, DropTargetIndex(e));
            if (moved != null)
            {
                // 移した行を、そのまま選択しておく
                macroListBox.UnselectAll();
                foreach (int index in moved)
                {
                    macroListBox.SelectedItems.Add(recordBoxVM.MacroSteps[index]);
                }

                macroListBox.ScrollIntoView(recordBoxVM.MacroSteps[moved[0]]);
            }

            e.Handled = true;
        }
    }
}
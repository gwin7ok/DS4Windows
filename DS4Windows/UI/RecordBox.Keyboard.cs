using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>キーの記録の開始・停止とキー入力の処理。</summary>
    public partial class RecordBox
    {
        private void ToggleRecording()
        {
            bool recording = recordBoxVM.Recording = !recordBoxVM.Recording;
            if (recording)
            {
                recordBoxVM.SetMacroRecordingActive(true);
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

                ds4.Start();
                recordBoxVM.ControlsEnabled = false;
                recordBoxVM.Sw.Restart();
                this.Focus();
                StartMouseRecording();
            }
            else
            {
                recordBoxVM.SetMacroRecordingActive(false);
                recordBoxVM.AppendIndex = -1;
                StopMouseRecording();
                ds4.Stop();
                recordBtn.Content = "Record";
                recordBoxVM.Sw.Stop();

                recordBoxVM.ControlsEnabled = true;
            }

            recordBoxVM.EditMacroIndex = -1;
            macroListBox.UnselectAll();
            recordBoxVM.MacroStepIndex = -1;
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
                    DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(value, tempKey.ToString(),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActDown, DS4Windows.Processing.Macro.MacroStep.StepOutput.Key);
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
                    DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(value, tempKey.ToString(),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActUp, DS4Windows.Processing.Macro.MacroStep.StepOutput.Key);
                    recordBoxVM.AddMacroStep(step);
                    recordBoxVM.KeysdownMap.Remove(value);
                }
                else if (RecordBoxViewModel.KeydownOverrides.Contains(value))
                {
                    DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(value, tempKey.ToString(),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActDown, DS4Windows.Processing.Macro.MacroStep.StepOutput.Key);
                    recordBoxVM.AddMacroStep(step, ignoreDelay: true);

                    step = new DS4Windows.Processing.Macro.MacroStep(value, tempKey.ToString(),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActUp, DS4Windows.Processing.Macro.MacroStep.StepOutput.Key);
                    recordBoxVM.AddMacroStep(step, ignoreDelay: true);
                }

                e.Handled = true;
                //Console.WriteLine(e.Key);
                //Console.WriteLine(e.SystemKey);
            }
        }

        private void WaitIUD_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                IntegerUpDown? integerUpDown = sender as IntegerUpDown;
                integerUpDown?.GetBindingExpression(IntegerUpDown.ValueProperty)?.UpdateSource();

                ListBoxItem? lbitem = macroListBox.ItemContainerGenerator.ContainerFromIndex(recordBoxVM.EditMacroIndex)
                        as ListBoxItem;
                if (lbitem != null) lbitem.ContentTemplate = this.FindResource("DisplayTemplate") as DataTemplate;
                recordBoxVM.EditMacroIndex = -1;
            }
        }
    }
}

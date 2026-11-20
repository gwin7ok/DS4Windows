using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using DS4Windows.Core.Model;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>マウスの記録（ボタン・移動・待ち時間）。</summary>
    public partial class RecordBox
    {
        private void FourMouseBtnAction()
        {
            int value = 259;
            recordBoxVM.KeysdownMap.TryGetValue(value, out bool isdown);
            if (!isdown)
            {
                DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(value, DS4Windows.Processing.Macro.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActDown, DS4Windows.Processing.Macro.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Add(value, true);
            }
            else
            {
                DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(value, DS4Windows.Processing.Macro.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActUp, DS4Windows.Processing.Macro.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Remove(value);
            }
        }

        private void FiveMouseBtnAction()
        {
            int value = 260;
            recordBoxVM.KeysdownMap.TryGetValue(value, out bool isdown);
            if (!isdown)
            {
                DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(value, DS4Windows.Processing.Macro.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActDown, DS4Windows.Processing.Macro.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Add(value, true);
            }
            else
            {
                DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(value, DS4Windows.Processing.Macro.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActUp, DS4Windows.Processing.Macro.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Remove(value);
            }
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

                DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(value, DS4Windows.Processing.Macro.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActDown, DS4Windows.Processing.Macro.MacroStep.StepOutput.Button);
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

                DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(value, DS4Windows.Processing.Macro.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                            DS4Windows.Processing.Macro.MacroStep.StepType.ActUp, DS4Windows.Processing.Macro.MacroStep.StepOutput.Button);
                recordBoxVM.AddMacroStep(step);
                recordBoxVM.KeysdownMap.Remove(value);
                e.Handled = true;
            }
        }

        // マウス移動のステップを、選択した位置の前（選択がなければ末尾）へ入れ、選択する。座標は、ダブルクリックで直す
        private void StartMouseRecording()
        {
            if (!recordBoxVM.RecordMouseMovement) return;

            limitStopRequested = false;
            pointerRecorder = mouseHook.CreateRecorder((DS4Windows.Processing.Macro.MacroPointerMoveMode)recordBoxVM.PointerRecordModeIndex);
            mouseHook.PointerMoved += MouseHook_PointerMoved;
            mouseHook.ButtonChanged += MouseHook_ButtonChanged;
            if (!mouseHook.Start())
            {
                StopMouseRecording();
            }
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

            recordBoxVM.AddMacroStep(new DS4Windows.Processing.Macro.MacroStep(value, DS4Windows.Processing.Macro.MacroParser.InputName(value, recordBoxVM.MacroOutType),
                isDown ? DS4Windows.Processing.Macro.MacroStep.StepType.ActDown : DS4Windows.Processing.Macro.MacroStep.StepType.ActUp, DS4Windows.Processing.Macro.MacroStep.StepOutput.Button));
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

                ToggleRecording();
                recordBoxVM.NotifyMoveRecordLimitReached(DS4Windows.Processing.Macro.MacroPointerRecorder.MaxMoveSteps);
            }));
        }

        private void AddMouseMoveStep()
        {
            if (recordBoxVM.Recording) return;

            if (recordBoxVM.EditMacroIndex > -1)
            {
                UpdateDataRevertTemplate();
            }

            // 追加するステップの基準は、「Record mouse movement」の下の一覧の、そのときの選択に合わせる
            int index = recordBoxVM.AddPointerMoveStep(recordBoxVM.MacroStepIndex, (DS4Windows.Processing.Macro.MacroPointerMoveMode)recordBoxVM.PointerRecordModeIndex);
            macroListBox.SelectedIndex = index;
            macroListBox.ScrollIntoView(recordBoxVM.MacroSteps[index]);
        }

        private void AddWaitStep()
        {
            if (recordBoxVM.Recording) return;

            // 行を選択していれば、いちばん上の選択行の前へ。選択がなければ、いちばん下へ足す
            int index = recordBoxVM.MacroStepIndex >= 0 ? recordBoxVM.MacroStepIndex : recordBoxVM.MacroSteps.Count;
            DS4Windows.Processing.Macro.MacroStep step = new DS4Windows.Processing.Macro.MacroStep(400, "Wait 100ms",
                        DS4Windows.Processing.Macro.MacroStep.StepType.Wait, DS4Windows.Processing.Macro.MacroStep.StepOutput.None);
            recordBoxVM.InsertMacroStep(index, step);
        }
    }
}
